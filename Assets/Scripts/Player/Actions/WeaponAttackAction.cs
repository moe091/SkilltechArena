using UnityEngine;

public class WeaponAttackAction : PlayerActionBase
{
    PlayerPrediction _pp;
    WeaponController _weapon;

    private void Awake()
    {
        _pp = GetComponent<PlayerPrediction>();
        _weapon = GetComponent<WeaponController>();
    }


    //Called when an action first starts. Includes references to context(mutable and regular) as well as currentVel, just like PlayerMovers, because actions can effect movement and other fields just like movers can.
    //The main difference between actions and movers is that actions are bound to a single key and they work off of the shared action timer(can't shoot and reload at the same time, for example).
    public override bool StartAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {
        Debug.Log("[DEBUG][WeaponAttackAction] StartAction called. mut.curAmmo = " + mut.curAmmo);
        if (mut.curAmmo > 0)
        {
            mut.actionTickTimer = Mathf.RoundToInt(_weapon.cooldownSeconds * (float)_pp.TimeManager.TickRate); // <---- THIS IS LINE 28
            mut.curAmmo -= _weapon.TryFire(context.rdTick, input.lookAngleDeg, context.isReplayed, ref currentVel);
            GameManager.HUDManager.SetAmmoAmount(mut.curAmmo);

            return true;
        }

        return false;
    }

    //Called each tick while the action is active(between button press and the end of its duration). This may be needed for some actions, e.g. if I have a "gattling gun" ability that rapid-fires for the whole duration and also
    //causes some player pushback/recoil each tick.
    public override void ContinueAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {

    }


    //called when action completes, for actions that have effects at the end of their duration(e.g. reload only sets curAmmo=maxAmmo after it's duration ends)
    public override void EndAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {

    }

}