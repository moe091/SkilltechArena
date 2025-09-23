using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalMover : PlayerMoverBase
{


    public override void DoMovement(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {
        if (mut.dashTimer <= 0f) //Mover is disabled during dash (for now, gonna change this later probably)
        { 
            float targetSpeed = input.horizontalInput * _stats.maxMoveSpeed;
            bool hasInput = Mathf.Abs(targetSpeed) > 0.01f;
            float accel = context.isGrounded
                ? (hasInput ? _stats.acceleration : _stats.deceleration)
                : (hasInput ? _stats.airAcceleration : _stats.airDeceleration);
            currentVel.x = Mathf.MoveTowards(currentVel.x, targetSpeed, accel * context.dt);
        }

    }

}
