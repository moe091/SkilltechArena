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
            mut.dashTimer = dashDuration + dashCooldown; 
            int dir = Mathf.Abs(input.horizontalInput) > 0.01f ? (input.horizontalInput > 0f ? 1 : -1) : context.facing;
            currentVel.x = dir * dashSpeed;
            currentVel.y = currentVel.y * 0.75f;
        }

        if (mut.dashTimer > 0f)
        {
            if (mut.dashTimer > dashCooldown)
            {
                int dir = Mathf.Abs(input.horizontalInput) > 0.01f ? (input.horizontalInput > 0f ? 1 : -1) : context.facing;
                currentVel.x = currentVel.x + dir * (dashSpeed / 20);
                currentVel.y = currentVel.y * 0.8f;
            }
            mut.dashTimer -= context.dt;
        }

        GameManager.HUDManager.UpdateDashAbility(mut.dashTimer, dashCooldown + dashDuration);
    }

}
