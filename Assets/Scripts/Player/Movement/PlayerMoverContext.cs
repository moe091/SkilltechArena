using UnityEngine;

public struct PlayerMoverContext
{
    public int rdTick;
    public float dt;
    public bool isGrounded;
    public int facing;
    public bool isReplayed;
}

public struct PlayerMutableContext
{
    public float dashTimer;
    public float jumpBufferTimer;
    public float coyoteTimer;
    public int actionTickTimer;
    public int curAmmo;
}
