using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerPrediction : TickNetworkBehaviour
{
    private PlayerInputCollector _inputCollector;

    [Header("Horizontal Movement")]
    public float maxMoveSpeed = 8f;
    public float acceleration = 40f;
    public float deceleration = 60f;   // when target speed is 0
    public float airAcceleration = 30f; // optional: lower accel in air
    public float airDeceleration = 40f;

    [Header("Jumping")]
    public float jumpForce = 14f;
    public float gravity = -40f;          // manual gravity (negative)
    public float coyoteTime = 0.10f;      // jump allowed shortly after leaving ground
    public float jumpBufferTime = 0.10f;  // queue jump shortly before landing
    public bool variableJump = true;      // short hop if jump not held
    public float jumpCutMultiplier = 0.5f; // how strongly we cut jump when not held

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

    // Simulated state (kept explicit for prediction)
    private Vector2 velocity;
    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashTimer;
    private float shootTimer;
    private float shotCooldown = 1f;
    private int facing = 1;               // -1 left, +1 right
    private float lookAngleDeg;
    private float weaponAngleDeg; // final, post-mirror angle we actually render


    private uint _lastReplicateTick;
    private PredictionRigidbody2D _predictionBody = new();

    private PlayerController _playerController;
    private WeaponController _weaponController;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Animator _animator;


    private void Awake()
    {
        _inputCollector = GetComponent<PlayerInputCollector>();
        rb = GetComponent<Rigidbody2D>();
        _predictionBody.Initialize(rb); // important: initialize with Rigidbody2D
        _playerController = GetComponent<PlayerController>();
        _weaponController = GetComponent<WeaponController>();
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
        _lastReplicateTick = input.GetTick();
        int rdTick = unchecked((int)_lastReplicateTick);


        float dt = (float)TimeManager.TickDelta;

        // Only the server and the local owner should simulate.
        bool simulateThisTick = IsServerStarted || IsOwner;
        if (!simulateThisTick)
            return; // remote clients do nothing; they'll update in [Reconcile]




        lookAngleDeg = input.lookAngleDeg;
        int facing = (Mathf.Abs(lookAngleDeg) > 90f) ? -1 : 1;

        if (facing == 1)
        {
            _spriteRenderer.flipX = false;
            _weaponController.SetWeaponInverted(false);
        } else
        {
            _spriteRenderer.flipX = true;
            _weaponController.SetWeaponInverted(true);
        }

        if (_weaponSlot)
            _weaponSlot.localRotation = Quaternion.Euler(0f, 0f, lookAngleDeg);




        // --- Buffered inputs & timers ---
        if (input.jumpPressed)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);

        // Grounded BEFORE velocity changes (predicted physics world)
        UpdateGrounded();
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer = Mathf.Max(0f, coyoteTimer - dt);

        // Start from current predicted velocity
        Vector2 currentVel = rb.velocity;

        // FIRE gate based on predicted tick cooldown
        if (shootTimer <= 0f && input.attack1Pressed)
        {
            bool isReplayed = state.HasFlag(ReplicateState.Replayed);
            shootTimer = shotCooldown;
            _weaponController.TryFire(rdTick, input.lookAngleDeg, isReplayed, ref currentVel);
        }
        if (shootTimer > 0f) shootTimer -= dt;

        // --- Dash ---
        if (dashTimer <= 0f && input.dashPressed)
        {
            dashTimer = dashDuration;
            int dir = Mathf.Abs(input.horizontalInput) > 0.01f ? (input.horizontalInput > 0f ? 1 : -1) : facing;
            currentVel.x = dir * dashSpeed;
        }
        if (dashTimer > 0f) dashTimer -= dt;
        else
        {
            float targetSpeed = input.horizontalInput * maxMoveSpeed;
            bool hasInput = Mathf.Abs(targetSpeed) > 0.01f;
            float accel = isGrounded
                ? (hasInput ? acceleration : deceleration)
                : (hasInput ? airAcceleration : airDeceleration);
            currentVel.x = Mathf.MoveTowards(currentVel.x, targetSpeed, accel * dt);
        }

        // --- Jump / gravity ---
        bool didStartJumpThisTick = false;
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            currentVel.y = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            didStartJumpThisTick = true;
        }
        else
        {
            currentVel.y += gravity * dt;
            if (variableJump && !input.jumpHeld && currentVel.y > 0f)
                currentVel.y += gravity * (1f - jumpCutMultiplier) * dt;
        }

        // If you re-enable ground-stick later, keep it here; for now it’s off.

        // --- Velocity targeting via force ---
        Vector2 dv = currentVel - rb.velocity;
        // (Optional) if you re-enable stick later:
        // if (isGrounded && !didStartJumpThisTick && dv.y > 0f) dv.y = 0f;

        _animator.SetFloat("speed", Mathf.Abs(currentVel.x));

        float mass = rb.mass > 0f ? rb.mass : 1f;
        float tickDt = Mathf.Max(dt, 1e-6f);
        Vector2 force = (mass * dv) / tickDt;
        if (force.sqrMagnitude > 0f)
            _predictionBody.AddForce(force);

        _predictionBody.Simulate();  // <-- only owner/server runs this

        

        velocity = currentVel; // for reconcile payload
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
            shootTimer,
            lookAngleDeg
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
        shootTimer = rd.ShootTimer;
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




    private void UpdateGrounded()
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
    }

    //NOTE:: this really doesn't belong here but because of reconciliation it's a lot easier to handle weapon cooldowns from within playerPrediction
    public void SetShotCooldown(float cd)
    {
        shotCooldown = cd;
    }

}
