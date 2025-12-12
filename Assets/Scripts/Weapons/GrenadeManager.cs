using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeManager : NetworkBehaviour //GrenadeManager is a per-player class, not a universal manager
{
    [SerializeField] private NetworkObject grenadePrefab;

    private PlayerPrediction _pp;
    private readonly SyncVar<int> _grenadesLeft = new SyncVar<int>(5);
    private float _nextGrenadeReadyTime = -1f;
    private float _cooldown = -1f;

    [SerializeField] private int throwStrength = 50;
    [SerializeField] private int maxGrenades = 5;
    [SerializeField] private float rechargeSeconds = 2f;
    [SerializeField] private float lifetime = 1f;

    private Coroutine _rechargeCo;

    public int GrenadesLeft => _grenadesLeft.Value;


    private void Awake()
    {
        _grenadesLeft.OnChange += OnGrenadesChanged;
        _pp = GetComponent<PlayerPrediction>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _grenadesLeft.Value = maxGrenades;
    }

    private void OnDisable()
    {
        if (_rechargeCo != null)
        {
            StopCoroutine(_rechargeCo);
            _rechargeCo = null;
        }
    }

    private void Update()
    {
        if (!(IsServerInitialized || IsOwner))
            return;

        if (GrenadesLeft < maxGrenades)
        {
            if (_cooldown <= 0f)
            {
                if (IsServerInitialized)
                    _grenadesLeft.Value = Mathf.Min(maxGrenades, _grenadesLeft.Value + 1);

                _cooldown = rechargeSeconds; //whether we have another grenade on cooldown or not, set cooldown. It will only tick down if we don't have max grenades
            }
            else
            {
                _cooldown -= Time.deltaTime;
            }
        }
        if (IsOwner)
        {
            GameManager.HUDManager.UpdateSecondaryAbility(GrenadesLeft, _cooldown, rechargeSeconds);
        }
    }


    private void OnGrenadesChanged(int prev, int next, bool asServer)
    {
        GameManager.HUDManager.SetGrenadeCount(next);
        _cooldown = rechargeSeconds;
    }



    [Server]
    public void SetGrenadeCount(int count)
    {
        _grenadesLeft.Value = count;
    }

    public void DecreaseGrenadeCount()
    {
        _grenadesLeft.Value = Mathf.Max(0, _grenadesLeft.Value - 1);
    }


    [ServerRpc(RequireOwnership = true)]
    public void ServerTryThrow(Vector2 pos, Vector2 vel, float aimDeg, uint tick) //vel is the players velocity, so it can be added to the player throw velocity
    {
        if (_grenadesLeft.Value <= 0)
        {
            // Optionally: send TargetRpc feedback (deny SFX) to owner here
            return;
        }


        NetworkObject grenadeNO = Instantiate(
            grenadePrefab,
            new Vector3(pos.x, pos.y, 0f),
            Quaternion.Euler(0f, 0f, aimDeg) // optional; just orients sprite
        );


        int explodeTick = (int)(tick + Math.Round(lifetime * _pp.TimeManager.TickRate));
        Grenade grenadeComp = grenadeNO.GetComponent<Grenade>();
        grenadeComp.explodeTick.Value = explodeTick;

        Spawn(grenadeNO);

        Rigidbody2D rb = grenadeNO.GetComponent<Rigidbody2D>();
        float rad = aimDeg * Mathf.Deg2Rad;

        Debug.Log("THROWING GRENADE AT ANGLE: " + rad);
        //rb.velocity = new Vector2((Mathf.Cos(rad) * throwStrength) + (vel.x * 0.2f), (Mathf.Sin(rad) * throwStrength) + (vel.y * 0.2f)); // Add Player velocity to throw
        rb.velocity = new Vector2((Mathf.Cos(rad) * throwStrength), (Mathf.Sin(rad) * throwStrength)); // Ignore player velocity

        DecreaseGrenadeCount();

        Debug.Log("Threw a grenade. grenades left = " + GrenadesLeft);

    }
}
