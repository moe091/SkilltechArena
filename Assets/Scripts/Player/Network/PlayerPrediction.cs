using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Windows;

/// <summary>
/// //////////////// NEXT UP: Route curAmmo through CSP/ReplicateState.
/// //////////////// THEN: Rework weaponController? Or just implement grenades, not using weaponController, and figure out a better way. Then change regular weapons to work the same way.
/// </summary>


public class PlayerPrediction : TickNetworkBehaviour
{
    private PlayerInputCollector _inputCollector;


    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;

    [Header("Ground Check")]
    public Transform groundCheck;         // place at the feet
    public float groundCheckRadius = 0.12f;
    public LayerMask groundLayer;

    [Header("Weapon")]
    public Transform _weaponSlot;
    public Transform _sprite;
    private Rigidbody2D rb;

    [Header("Actions")] 
    public PlayerActionBase attack1Action;
    public PlayerActionBase attack2Action;
    public PlayerActionBase reloadAction;


    [Header("Other")]
    // Simulated state (kept explicit for prediction)
    private Vector2 velocity;
    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashTimer; 
    private int actionTickTimer;      // ticks remaining until next allowed action
    public int shotCooldownTicks;    // ticks for the cooldown
    private float lookAngleDeg;
    private int curAmmo;

    private int actionBufferTicks = 20;

    private PlayerMoverContext _moverContext = new PlayerMoverContext();
    private uint _lastReplicateTick;
    private PredictionRigidbody2D _predictionBody = new();
    private PlayerActionBase _currentAction = null;

    private PlayerController _playerController;
    private WeaponController _weaponController;
    private PlayerStats _stats;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMoverBase[] _movers;



    private void Awake()
    {
        _inputCollector = GetComponent<PlayerInputCollector>();
        rb = GetComponent<Rigidbody2D>();
        _predictionBody.Initialize(rb); // important: initialize with Rigidbody2D
        _playerController = GetComponent<PlayerController>();
        _weaponController = GetComponent<WeaponController>();
        _stats = GetComponent<PlayerStats>();
    }


    public override void OnStartNetwork()
    {
        //Rigidbodies need tick and postTick.
        base.SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
        {
            //rb.isKinematic = true;
            //rb.interpolation = RigidbodyInterpolation2D.Interpolate; // smooth rendering
            return;
        }


        var cam = Camera.main;
        if (!cam) return;

        var follower = cam.GetComponent<CameraFollow>();
        if (!follower) follower = cam.gameObject.AddComponent<CameraFollow>();
        follower.target = transform; // follow this local player's transform

        GameManager.HUDManager.InitSecondaryAbil("UI/HUD/GrenadeHUD");
        GameManager.HUDManager.InitDashAbil("UI/HUD/DashHUD");
    }

    protected override void TimeManager_OnTick()
    {
        // Only the owner should produce inputs; others will receive state
        if (IsOwner) {
            _inputCollector.GatherInput();
        } else
        {

        }
            
        var input = IsOwner ? _inputCollector.RetrieveInput() : default; // PlayerInputData (IReplicateData)

        if (IsServerStarted || IsOwner)
        {
            PerformReplicate(input);
        }
    }

    protected override void TimeManager_OnPostTick()
    {

        CreateReconcile(); // send snapshot each tick (you can throttle later)
    }



    [Replicate]
    private void PerformReplicate(PlayerInputData input,
                              ReplicateState state = ReplicateState.Invalid,
                              Channel channel = Channel.Unreliable)
    {
        if (!(IsServerStarted || IsOwner))
            return; // remote clients do nothing; they'll update in [Reconcile]
        
        lookAngleDeg = input.lookAngleDeg;
        PlayerMutableContext mut = GetMutableContext();
        UpdateMoverContext(input.GetTick(), state.HasFlag(ReplicateState.Replayed));
        UpdateVisuals();  

        Vector2 currentVel = rb.velocity;

        
        //TODO:: move this into "HandleMovers" function to make things nice and clean
        for (int i = 0; i < _movers.Length; i++)
        {
            var mover = _movers[i];
            if (mover)
                mover.DoMovement(input, _moverContext, ref mut, ref currentVel);
        }

        HandleActions(input, state.HasFlag(ReplicateState.Replayed), ref mut, ref currentVel);

        jumpBufferTimer = mut.jumpBufferTimer;
        coyoteTimer = mut.coyoteTimer;
        dashTimer = mut.dashTimer;
        actionTickTimer = mut.actionTickTimer;
        curAmmo = mut.curAmmo;


        // Calculate and apply force based on changes made to currentVel by movers(and TryFire)
        Vector2 dv = currentVel - rb.velocity;

        float mass = rb.mass > 0f ? rb.mass : 1f;
        float tickDt = Mathf.Max(_moverContext.dt, 1e-6f);
        Vector2 force = (mass * dv) / tickDt;
        if (force.sqrMagnitude > 0f)
            _predictionBody.AddForce(force);


        _predictionBody.Simulate(); 
        velocity = currentVel; // for reconcile payload
    }

    private void HandleActions(PlayerInputData input, bool isReplayed, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {
        int now = unchecked((int)TimeManager.Tick);

        // 1) Record edge inputs into per-action buffers.
        //    If pressed again while already buffered, extend the buffer (keep the later deadline).
        if (input.attack1Pressed)
            attack1Action.bufferedUntil = Mathf.Max(attack1Action.bufferedUntil, now + actionBufferTicks);

        if (input.attack2Pressed)
            attack2Action.bufferedUntil = Mathf.Max(attack2Action.bufferedUntil, now + actionBufferTicks);

        if (input.reloadPressed)
            reloadAction.bufferedUntil = Mathf.Max(reloadAction.bufferedUntil, now + actionBufferTicks);



        // 2) If the shared action gate just opened, finish the previous action.
        if (mut.actionTickTimer <= 0 && _currentAction != null)
        {
            _currentAction.EndAction(input, _moverContext, ref mut, ref currentVel);
            _currentAction = null;
        }


        // 3) If idle, try to start something.
        if (mut.actionTickTimer <= 0)
        {
            // Priority: attack, then reload. Check buffered windows first.
            // Start when buffer is still valid (>= now). If start succeeds, consume buffer.
            if (attack1Action.bufferedUntil >= now)
            {
                if (attack1Action.StartAction(input, _moverContext, ref mut, ref currentVel))
                {
                    attack1Action.bufferedUntil = -1;   // consume
                    _currentAction = attack1Action;
                    goto TickDown;
                }
                // If StartAction failed (e.g., no ammo), keep buffer until it expires.
            }

            if (attack2Action.bufferedUntil >= now)
            {
                if (attack2Action.StartAction(input, _moverContext, ref mut, ref currentVel))
                {
                    attack2Action.bufferedUntil = -1;   // consume
                    _currentAction = attack2Action;
                    goto TickDown;
                }
                // If StartAction failed (e.g., no ammo), keep buffer until it expires.
            }

            if (reloadAction.bufferedUntil >= now)
            {
                if (reloadAction.StartAction(input, _moverContext, ref mut, ref currentVel))
                {
                    reloadAction.bufferedUntil = -1;    // consume
                    _currentAction = reloadAction;
                    goto TickDown;
                }
            }

            // (Optional) If you want non-buffered immediate starts too, you could also
            // try direct edges here; but because we set buffers on edges above,
            // those edges are already covered by the buffered checks.
        }

    TickDown:
        // 4) Tick the shared gate down on the mutable state.
        if (mut.actionTickTimer > 0)
            mut.actionTickTimer -= 1;

        // (Optional) Expire old buffers; harmless to leave them, but this keeps things tidy.
        if (attack1Action.bufferedUntil < now)
            attack1Action.bufferedUntil = -1;
        if (reloadAction.bufferedUntil < now)
            reloadAction.bufferedUntil = -1;
    }


    public override void CreateReconcile()
    {
        var rd = new PlayerReconcileData(
            _predictionBody,
            velocity,
            isGrounded,
            coyoteTimer,
            jumpBufferTimer,
            dashTimer,
            actionTickTimer,
            lookAngleDeg,
            curAmmo
        );

        if (IsServerStarted || IsOwner)
            PerformReconcile(rd);
    }

    [Reconcile]
    private void PerformReconcile(PlayerReconcileData rd, Channel channel = Channel.Unreliable)
    {
        if (!IsOwner) {
            velocity = rd.Velocity;
            isGrounded = rd.IsGrounded;
        }
        coyoteTimer = rd.CoyoteTimer;
        jumpBufferTimer = rd.JumpBufferTimer;
        dashTimer = rd.DashTimer;
        actionTickTimer = rd.ActionTickTimer;
        lookAngleDeg = rd.LookAngleDeg;


        int facing = (Mathf.Abs(lookAngleDeg) > 90f) ? -1 : 1;

        if (facing == 1)
        {
            _spriteRenderer.flipX = false;
            _weaponController.SetWeaponInverted(false);
        }
        else
        {
            _spriteRenderer.flipX = true;
            _weaponController.SetWeaponInverted(true);
        }

        if (_weaponSlot)
            _weaponSlot.localRotation = Quaternion.Euler(0f, 0f, lookAngleDeg);

        _predictionBody.Reconcile(rd.Body);
    }




    private bool UpdateGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            // Fallback: small circle at feet (object's position)
            isGrounded = Physics2D.OverlapCircle(transform.position, groundCheckRadius, groundLayer);
        }

        return isGrounded;
    }

    //NOTE:: this really doesn't belong here but because of reconciliation it's a lot easier to handle weapon cooldowns from within playerPrediction
    public void SetShotCooldown(float cd)
    {
        shotCooldownTicks = Mathf.RoundToInt(cd * (float)TimeManager.TickRate);
    }


    private PlayerMutableContext GetMutableContext()
    {
        PlayerMutableContext mut = new PlayerMutableContext();
        mut.jumpBufferTimer = jumpBufferTimer;
        mut.coyoteTimer = coyoteTimer;
        mut.dashTimer = dashTimer;
        mut.actionTickTimer = actionTickTimer;
        mut.curAmmo = curAmmo;

        return mut;
    }
    private void UpdateMoverContext(uint lastReplicateTick, bool isReplayed)
    {
        _moverContext.rdTick = unchecked((int)lastReplicateTick);
        _moverContext.dt = (float)TimeManager.TickDelta;
        _moverContext.isReplayed = isReplayed;
        
        _moverContext.facing = (Mathf.Abs(lookAngleDeg) > 90f) ? -1 : 1;

        _moverContext.isGrounded = UpdateGrounded();
    }

    private void UpdateVisuals()
    {
        if (_moverContext.facing == 1)
        {
            _spriteRenderer.flipX = false;
            _weaponController.SetWeaponInverted(false);
        }
        else
        {
            _spriteRenderer.flipX = true;
            _weaponController.SetWeaponInverted(true);
        }

        if (_weaponSlot)
            _weaponSlot.localRotation = Quaternion.Euler(0f, 0f, lookAngleDeg);

        _animator.SetFloat("speed", Mathf.Abs(rb.velocity.x));
    }

    public void SetAmmo(int ammo) //THIS WILL CAUSE AN ISSUE IF CALLED VIA MOVER OR PLAYERACTION, BECAUSE IT WILL GET OVERWRITTEN BY MUT
    {
        curAmmo = ammo;
    }
}
