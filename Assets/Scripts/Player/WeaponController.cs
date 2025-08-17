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

    [SerializeField] private Collider2D shooterCollider;   // assign your player's main collider
    public Collider2D ShooterCollider => shooterCollider;  // expose for Shotgun

    private void Awake()
    {
        // Subscribe to change notifications (server & clients)
        _equippedId.OnChange += OnEquippedIdChanged;
    }

    /// <summary>
    /// Equip a weapon on the SERVER. This sets the synced ID;
    /// clients get OnEquippedIdChanged and rebuild visuals.
    /// </summary>
    [Server]
    public void Equip(WeaponDefinition weapon)
    {
        if (weapon == null) return;

        Debug.Log($"[Server] Equipping {weapon.name}. Current equippedId: {_equippedId.Value}");
        _equippedId.Value = weapon.weaponId;   // triggers OnEquippedIdChanged everywhere
        Debug.Log($"[Server] New equippedId: {_equippedId.Value}");

        // Server initializes authoritative state
        curAmmo = weapon.maxAmmo;
        nextAllowedFireTick = 0;
    }

    public void TryFire(int tick, float angleDegrees)
    {
        if (curWeapon == null)
        {
            Debug.Log("Can't Fire - no weapon equipped");
            return;
        }

        // Tick-gated rate of fire & ammo (basic MVP gating)
        int fireInterval = curWeapon.GetFireIntervalTicks(TimeManager.TickRate);
        if (tick < nextAllowedFireTick || curAmmo <= 0)
            return;

        nextAllowedFireTick = tick + fireInterval;
        curAmmo--;

        float rad = angleDegrees * Mathf.Deg2Rad;
        Vector2 aimDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;


        // Forward to runtime behavior (Shotgun)
        if (_weapon != null)
            _weapon.TryFire(tick, aimDir.normalized);
        else
            Debug.LogWarning("No IWeaponRuntime found on weapon view prefab.");
    }

    // v4 OnChange signature: (prev, next, asServer)
    private void OnEquippedIdChanged(WeaponId prev, WeaponId next, bool asServer)
    {
        Debug.Log("OnEquppiedIdChanged Called!");
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
}
