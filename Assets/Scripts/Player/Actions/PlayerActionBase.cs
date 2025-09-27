using UnityEngine;

public abstract class PlayerActionBase : MonoBehaviour
{
    protected PlayerStats _stats;
    public int tickDuration; //stores the duration of the action in ticks
    public int assignedId; //stores the unique ID used to identify instances of actions
    public int bufferedUntil = -1;

    private void Awake()
    {
        _stats = GetComponent<PlayerStats>();
    }

    //Called when an action first starts. Includes references to context(mutable and regular) as well as currentVel, just like PlayerMovers, because actions can effect movement and other fields just like movers can.
    //The main difference between actions and movers is that actions are bound to a single key and they work off of the shared action timer(can't shoot and reload at the same time, for example).
    public abstract bool StartAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel);

    //Called each tick while the action is active(between button press and the end of its duration). This may be needed for some actions, e.g. if I have a "gattling gun" ability that rapid-fires for the whole duration and also
    //causes some player pushback/recoil each tick.
    public abstract void ContinueAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel);

    //called when action completes, for actions that have effects at the end of their duration(e.g. reload only sets curAmmo=maxAmmo after it's duration ends)
    public abstract void EndAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel);

}