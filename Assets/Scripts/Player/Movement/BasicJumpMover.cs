using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicJumpMover :  PlayerMoverBase 
{
    public float jumpForce = 14f;
    public bool variableJump = true;      // short hop if jump not held
    public float jumpCutMultiplier = 0.5f; // how strongly we cut jump when not held
    public float jumpBufferTime = 0.10f;  // queue jump shortly before landing
    public float coyoteTime = 0.10f;      // jump allowed shortly after leaving ground


    public override void DoMovement(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {
        if (context.isGrounded)
            mut.coyoteTimer = mut.coyoteTimer = coyoteTime;
        else
            mut.coyoteTimer = mut.coyoteTimer = Mathf.Max(0f, mut.coyoteTimer - context.dt);

        if (input.jumpPressed)
            mut.jumpBufferTimer = jumpBufferTime;
        else
            mut.jumpBufferTimer = Mathf.Max(0f, mut.jumpBufferTimer - context.dt);


        if (mut.jumpBufferTimer > 0f && mut.coyoteTimer > 0f)
        {
            currentVel.y = jumpForce;
            mut.jumpBufferTimer = 0f;
            mut.coyoteTimer = 0f;
        }
        else
        {
            currentVel.y += _stats.gravity * context.dt;
            if (variableJump && !input.jumpHeld && currentVel.y > 0f)
                currentVel.y += _stats.gravity * (1f - jumpCutMultiplier) * context.dt;
        }

    }

}
