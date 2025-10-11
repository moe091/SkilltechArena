using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeManager : NetworkBehaviour
{
    [SerializeField] private NetworkObject grenadePrefab;

    private PlayerPrediction _pp;
    private readonly SyncVar<int> _grenadesLeft = new SyncVar<int>(2);
    private int throwStrength = 45;


    private void Awake()
    {
        _grenadesLeft.OnChange += OnGrenadesChanged;
        _pp = GetComponent<PlayerPrediction>();
    }

    private void OnGrenadesChanged(int prev, int next, bool asServer)
    {
        GameManager.HUDManager.SetGrenadeCount(next);
    }

    public int GrenadesLeft => _grenadesLeft.Value;


    [Server]
    public void SetGrenades(int count)
    {
        _grenadesLeft.Value = count;
    }


    [ServerRpc(RequireOwnership = true)]
    public void ServerTryThrow(Vector2 pos, Vector2 vel, float aimDeg, uint tick) //vel is the players velocity, so it can be added to the player throw velocity
    {
        NetworkObject grenadeNO = Instantiate(
            grenadePrefab,
            new Vector3(pos.x, pos.y, 0f),
            Quaternion.Euler(0f, 0f, aimDeg) // optional; just orients sprite
        );

        float lifetime = 1.5f;

        int explodeTick = (int)(tick + Math.Round(lifetime * _pp.TimeManager.TickRate));
        Grenade grenadeComp = grenadeNO.GetComponent<Grenade>();
        grenadeComp.explodeTick.Value = explodeTick;

        Spawn(grenadeNO);

        Rigidbody2D rb = grenadeNO.GetComponent<Rigidbody2D>();
        float rad = aimDeg * Mathf.Deg2Rad;
        rb.velocity = new Vector2((Mathf.Cos(rad) * throwStrength) + vel.x, (Mathf.Sin(rad) * throwStrength) + vel.y);
    }
}
