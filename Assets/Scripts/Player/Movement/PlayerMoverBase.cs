using UnityEngine;

public abstract class PlayerMoverBase : MonoBehaviour
{
    protected PlayerStats _stats;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }


    public abstract void DoMovement(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel);

}