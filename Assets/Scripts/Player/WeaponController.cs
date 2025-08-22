using FishNet.Object;                    // NetworkBehaviour, [Server]
using FishNet.Object.Synchronizing;      // SyncVar<T>
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    [Header("Mount Point")]
    public Transform WeaponSlot;               // assign in Inspector

    [Header("Runtime (local)")]
    public WeaponDefinition curWeapon;         // resolved locally from ID
    int curAmmo;
    int nextAllowedFireTick;

    GameObject _viewInstance;

    private readonly SyncVar<WeaponId> _equippedId = new SyncVar<WeaponId>();
    private IWeapon _weapon;
    private PlayerPrediction _playerPrediction;
    [SerializeField] private Collider2D shooterCollider;   // assign your player's main collider

    private void Awake()
    {
        // Subscribe to change notifications (server & clients)
        _equippedId.OnChange += OnEquippedIdChanged;
        _playerPrediction = GetComponent<PlayerPrediction>();
    }

    /// <summary>
    /// Equip a weapon on the SERVER. This sets the synced ID;
    /// clients get OnEquippedIdChanged and rebuild visuals.
    /// </summary>
    [Server]
    public void Equip(WeaponDefinition weapon)
    {
        if (weapon == null) return;

        _equippedId.Value = weapon.weaponId;   // triggers OnEquippedIdChanged everywhere
        _playerPrediction.SetShotCooldown(weapon.secondsBetweenShots);
        // Server initializes authoritative state
        curAmmo = weapon.maxAmmo;
        nextAllowedFireTick = 0;
    }

    public Vector2 TryFire(int tick, float angleDegrees, bool isReplayed, ref Vector2 currentVel)
    {
        if (curWeapon == null)
        {
            Debug.Log("Can't Fire - no weapon equipped");
            return Vector2.zero;
        }

        // Tick-gated rate of fire & ammo (basic MVP gating)
        int fireInterval = curWeapon.GetFireIntervalTicks(TimeManager.TickRate);
        //if (tick < nextAllowedFireTick || curAmmo <= 0)
            //return Vector2.zero;

        nextAllowedFireTick = tick + fireInterval;
        curAmmo--;

        float rad = angleDegrees * Mathf.Deg2Rad;
        Vector2 aimDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;


        // Forward to runtime behavior (Shotgun)
        if (_weapon != null)
        {
            return _weapon.TryFire(tick, aimDir.normalized, isReplayed, ref currentVel);
        }
        else
        {
            Debug.LogWarning("No IWeaponRuntime found on weapon view prefab.");
            return Vector2.zero;
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void ServerFire(Vector2 pos, float aimDir, uint tick)
    {
        float timePassed = (float)base.TimeManager.TimePassed(tick, false);
        //passedTime = Mathf.Min(MAX_PASSED_TIME / 2f, passedTime); // add cap so that super laggy clients don't screw over other players
        Debug.Log("[WeaponController::ServerFire] called with timePassed = " + timePassed);
        _weapon?.SpawnProjectiles(pos, aimDir, timePassed);
        ObserverFire(pos, aimDir, tick);
    }

    [ObserversRpc(ExcludeOwner = true)]
    public void ObserverFire(Vector2 pos, float aimDir, uint tick)
    {
        float timePassed = (float)base.TimeManager.TimePassed(tick, false);
        //passedTime = Mathf.Min(MAX_PASSED_TIME / 2f, passedTime); // add cap so that super laggy clients don't screw over other players
        Debug.Log("[WeaponController::ServerFire] called with timePassed = " + timePassed);
        _weapon?.SpawnProjectiles(pos, aimDir, timePassed);
    }

    // v4 OnChange signature: (prev, next, asServer)
    private void OnEquippedIdChanged(WeaponId prev, WeaponId next, bool asServer)
    {
        // Resolve the actual ScriptableObject locally
        var db = WeaponDatabase.Load();
        curWeapon = db ? db.Get(next) : null;

        RebuildView();

        // Mirror ammo/cooldown locally for UI; server already set authoritative values
        if (curWeapon != null)
        {
            curAmmo = curWeapon.maxAmmo;
            nextAllowedFireTick = 0;
        }
    }

    private void RebuildView()
    {
        if (_viewInstance) Destroy(_viewInstance);

        if (curWeapon == null || curWeapon.viewPrefab == null || WeaponSlot == null)
            return;

        _viewInstance = Instantiate(curWeapon.viewPrefab, WeaponSlot, false);
        _viewInstance.transform.localPosition = Vector3.zero;
        _viewInstance.transform.localRotation = Quaternion.identity;
        _viewInstance.transform.localScale = Vector3.one;

        _weapon = _viewInstance.GetComponent<IWeapon>();
        if (_weapon != null)
            _weapon.Initialize(this);
    }

    [ServerRpc(RequireOwnership = true)]
    public void Server_Fire(Vector2 muzzlePos, float baseAngleDeg, int pelletCount,
                        float spreadDeg, float speed, float life, int seed, uint fireTick)
    {
        _weapon?.Server_Fire(muzzlePos, baseAngleDeg, pelletCount,
                                      spreadDeg, speed, life, seed, fireTick);
    }

    [ObserversRpc(ExcludeOwner = true)]
    public void Obs_Fire(Vector2 muzzlePos, float baseAngleDeg, int pelletCount,
                         float spreadDeg, float speed, float life, int seed, float passedTimeSeconds)
    {
        _weapon?.Obs_Fire(muzzlePos, baseAngleDeg, pelletCount,
                                   spreadDeg, speed, life, seed, passedTimeSeconds);
    }
}
