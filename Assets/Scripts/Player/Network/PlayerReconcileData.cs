using FishNet.Object.Prediction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerReconcileData : IReconcileData
{
    // Snapshot of the physics body handled by FishNet
    public PredictionRigidbody2D Body;

    // Your gameplay-side state that isn’t in the body snapshot
    public Vector2 Velocity;
    public bool IsGrounded;
    public float CoyoteTimer;
    public float JumpBufferTimer;
    public float DashTimer;
    public int ActionTickTimer;
    public float LookAngleDeg;
    public int CurAmmo;
    public sbyte ActionBufferTicks;
    public BufferedActionType BufferedAction;

    // Required plumbing
    private uint _tick;
    public PlayerReconcileData(PredictionRigidbody2D body, Vector2 vel, bool grounded,
                         float coyote, float jumpBuf, float dash, int actionTickTimer, float lookAngleDeg, int curAmmo, sbyte actionBufferTicks, BufferedActionType bufferedAction)
    {
        Body = body;
        Velocity = vel;
        IsGrounded = grounded;
        CoyoteTimer = coyote;
        JumpBufferTimer = jumpBuf;
        DashTimer = dash;
        ActionTickTimer = actionTickTimer;
        LookAngleDeg = lookAngleDeg;
        CurAmmo = curAmmo;
        ActionBufferTicks = actionBufferTicks;
        BufferedAction = bufferedAction;
        _tick = 0;
    }
    public void Dispose() { }
    public uint GetTick() => _tick;
    public void SetTick(uint value) => _tick = value;
}
