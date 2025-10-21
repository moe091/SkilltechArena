using UnityEngine;

public class GrenadeThrowAction : PlayerActionBase
{
    PlayerPrediction _pp;
    GrenadeManager _grenades;

    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioSource sfxSource; // optional; will be created if not set
    [SerializeField] private float xOffset = 1f;
    [SerializeField] private float yOffset = 1.3f;


    private void Awake()
    {
        _pp = GetComponent<PlayerPrediction>();
        _grenades = GetComponent<GrenadeManager>();
        //sfxSource = GetComponent<AudioSource>();
    }


    //Called when an action first starts. Includes references to context(mutable and regular) as well as currentVel, just like PlayerMovers, because actions can effect movement and other fields just like movers can.
    //The main difference between actions and movers is that actions are bound to a single key and they work off of the shared action timer(can't shoot and reload at the same time, for example).
    public override bool StartAction(PlayerInputData input, PlayerMoverContext context, ref PlayerMutableContext mut, ref Vector2 currentVel)
    {
        if (_grenades.GrenadesLeft <= 0)
        {
            Debug.Log("[GrenadeThrowAction.StartAction] No grenades left, can't throw!");
            return false;
        }

        Debug.Log($"{_grenades.GrenadesLeft} Grenades left. Throwing one!");
        mut.actionTickTimer = tickDuration;

        if (!context.isReplayed)
        {
            Vector2 handPosition = new Vector2(transform.position.x + (xOffset * context.facing), transform.position.y + yOffset);
            _grenades.ServerTryThrow(handPosition, currentVel, input.lookAngleDeg, _pp.TimeManager.Tick);
        }

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

    }
}