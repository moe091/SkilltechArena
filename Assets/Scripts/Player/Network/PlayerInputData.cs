using UnityEngine;
using FishNet.Object.Prediction; // <-- important

[System.Serializable]
public struct PlayerInputData : IReplicateData
{
    // Your fields
    public float horizontalInput;
    public bool jumpPressed;
    public bool jumpHeld;
    public bool attack1Pressed;
    public bool attack1Held;
    public bool attack2Pressed;
    public bool attack2Held;
    public bool reloadPressed;
    public Vector2 lookDirection;
    public float lookAngleDeg;
    public bool dashPressed;

    // Let FishNet manage the tick internally.
    // Keep this PRIVATE so you don't set it manually.
    private uint _tick;

    // ---- IReplicateData ----
    public void Dispose() { }          // no-op is fine
    public uint GetTick() => _tick;    // FishNet reads this
    public void SetTick(uint value)    // FishNet writes this
        => _tick = value;
}
