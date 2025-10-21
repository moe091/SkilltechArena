using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HorizontalDashMover : PlayerMoverBase
{
    public float dashSpeed = 38f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 5f;


    public override void DoMovement(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {
        if (mut.dashTimer <= 0f && input.dashPressed) //not already dashing, and dash button pressed
        {
            mut.dashTimer = dashDuration; 
            int dir = Mathf.Abs(input.horizontalInput) > 0.01f ? (input.horizontalInput > 0f ? 1 : -1) : context.facing;
            currentVel.x = dir * dashSpeed;
        }

        if (mut.dashTimer > 0f)
            mut.dashTimer -= context.dt;
    }

}
