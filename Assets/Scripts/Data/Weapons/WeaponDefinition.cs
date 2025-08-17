using UnityEngine;

public enum WeaponId
{
    None = 0,
    Shotgun = 1,
    // Pistol = 2, Rocket = 3, ...
}

[CreateAssetMenu(
    fileName = "WeaponDefinition",
    menuName = "Game/Weapons/WeaponDefinition",
    order = 0)]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public WeaponId weaponId = WeaponId.Shotgun;
    public string displayName = "Shotgun";
    public Sprite icon;                       // for UI
    public GameObject viewPrefab;             // visual-only prefab to mount on the player (sprite, muzzle FX, etc.)

    [Header("Tuning (gameplay)")]
    [Min(1)] public int maxAmmo = 8;
    [Min(0)] public int damage = 10;
    [Min(0)] public float recoilForce = 12f;  // impulse applied to player, in m/s (converted via your velocity targeting)
    [Min(1)] public int numProjectiles = 6;   // pellets per shot
    [Range(0f, 45f)] public float spreadDegrees = 15f;
    [Min(0f)] public float reloadTimeSeconds = 1.2f;

    [Header("Rate of fire")]
    [Tooltip("Seconds between shots. Use ticks in code = ceil(secondsBetweenShots * tickRate).")]
    [Min(0f)] public float secondsBetweenShots = 1.0f;

    // -------- helpers (read-only; no state) --------
    /// Convert secondsBetweenShots into ticks at runtime.
    public int GetFireIntervalTicks(int tickRate)
    {
        var t = Mathf.Max(1, Mathf.CeilToInt(secondsBetweenShots * Mathf.Max(1, tickRate)));
        return t;
    }

    /// Optional: compute per-pellet damage if you want per-shot damage to stay constant.
    public int GetDamagePerProjectile() => Mathf.Max(1, Mathf.RoundToInt(damage / Mathf.Max(1f, numProjectiles)));

#if UNITY_EDITOR
    private void OnValidate()
    {
        // clamp a few things to sane ranges in the inspector
        maxAmmo = Mathf.Max(1, maxAmmo);
        damage = Mathf.Max(0, damage);
        numProjectiles = Mathf.Max(1, numProjectiles);
        spreadDegrees = Mathf.Clamp(spreadDegrees, 0f, 89f);
        secondsBetweenShots = Mathf.Max(0f, secondsBetweenShots);
        reloadTimeSeconds = Mathf.Max(0f, reloadTimeSeconds);
    }
#endif
}
