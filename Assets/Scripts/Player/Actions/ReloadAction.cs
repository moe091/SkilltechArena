using UnityEngine;

public class ReloadAction : PlayerActionBase
{
    PlayerPrediction _pp;
    WeaponController _weapon;

    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioSource sfxSource; // optional; will be created if not set


    private void Awake()
    {
        _pp = GetComponent<PlayerPrediction>();
        _weapon = GetComponent<WeaponController>();
        sfxSource = GetComponent<AudioSource>();
    }


    //Called when an action first starts. Includes references to context(mutable and regular) as well as currentVel, just like PlayerMovers, because actions can effect movement and other fields just like movers can.
    //The main difference between actions and movers is that actions are bound to a single key and they work off of the shared action timer(can't shoot and reload at the same time, for example).
    public override bool StartAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {
        if (_weapon == null || _weapon.curWeapon == null || mut.curAmmo == _weapon.curWeapon.maxAmmo) 
            return false;

        int maxAmmo = (_weapon.curWeapon != null) ? _weapon.curWeapon.maxAmmo : 0;
        int reloadTicks = Mathf.Max(1, Mathf.RoundToInt(_weapon.GetReloadDuration() * (float)_pp.TimeManager.TickRate));

        mut.actionTickTimer = reloadTicks;

        if (_pp.IsOwner && !context.isReplayed)
            _weapon.PlayReloadSound();

        return true;
    }

    //Called each tick while the action is active(between button press and the end of its duration). This may be needed for some actions, e.g. if I have a "gattling gun" ability that rapid-fires for the whole duration and also
    //causes some player pushback/recoil each tick.
    public override void ContinueAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {

    }


    //called when action completes, for actions that have effects at the end of their duration(e.g. reload only sets curAmmo=maxAmmo after it's duration ends)
    public override void EndAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {
        Debug.Log("[ReloadAction.EndAction] Reload action complete!");
        mut.curAmmo = _weapon.curWeapon.maxAmmo;
        GameManager.HUDManager.SetAmmoAmount(mut.curAmmo);
    }
}