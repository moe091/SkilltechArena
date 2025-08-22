using FishNet;
using FishNet.Managing.Predicting;
using FishNet.Object;           // to access controller.ServerManager, controller.NetworkObject
using UnityEngine;
using UnityEngine.UIElements;

public class Shotgun : MonoBehaviour, IWeapon
{ 
    [Header("Setup")]
    // Option A (no NetworkObject): one generic prefab + data asset
    [SerializeField] private Projectile projectilePrefab;   // the visual/server prefab (no NetworkObject)
    [SerializeField] private ProjectileDef projectileDef;     // ScriptableObject with speed, lifetime, etc.

    [SerializeField] private Transform muzzle;               // optional; otherwise we’ll use transform.position

    [Header("Ballistics")]
    [SerializeField] private float projectileSpeed = 24f;    // units/sec (2D)
    [SerializeField] private int pelletCount = 8;                // number of pellets per shot
    [SerializeField] private float spreadDegrees = 30f;      // full cone width (e.g., 12° => ±6°)
    [SerializeField] private float speedVariance = 3f;      // full cone width (e.g., 12° => ±6°)
    [SerializeField] private float muzzleBackOffset = 0f;    // push muzzle slightly back if needed
    [SerializeField] private float recoilForce = 20f;    // push muzzle slightly back if needed
    [SerializeField] private int cooldown = 70;    // push muzzle slightly back if needed

    private int _lastShotTick = 0;


    // Injected at runtime
    private WeaponController _controller;
    private Animator _animator;
    [SerializeField] private AudioClip shootSfx;
    private AudioSource _audioSource;
    private PredictionManager _predict;


    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
        _predict = InstanceFinder.PredictionManager;
    }


    public void Initialize(WeaponController controller)
    {
        _controller = controller;
        if (muzzle == null) muzzle = transform; // fallback
    }

    /// <summary>
    /// Called by WeaponController after it has passed ROF/ammo gating.
    /// Owner can show VFX immediately; server actually spawns pellets.
    /// </summary>
    public Vector2 TryFire(int tick, Vector2 aimDir, bool isReplayed, ref Vector2 currentVel) 
    {
        Vector2 muzzlePos = muzzle ? (Vector2)muzzle.position : (Vector2)transform.position;

        float baseAngleDeg = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

        if (!isReplayed && _controller.IsOwner)
        {
            SpawnProjectiles(muzzlePos, baseAngleDeg, 0);
            _controller.ServerFire(muzzlePos, baseAngleDeg, _controller.TimeManager.Tick); //grab the tick HERE because serverFire is an RPC, we want to pass the tick from client
        }

        Vector2 recoilVector = aimDir.normalized * -recoilForce;
        float yRecoil = recoilVector.y;
        float xRecoil = recoilVector.x;

        float yDamper = currentVel.y * -0.5f;
        yRecoil = yRecoil + yDamper;

        currentVel += new Vector2(xRecoil, yRecoil);

        return (aimDir.normalized * -recoilForce);
    }




    public void SpawnProjectiles(Vector2 pos, float aimDir, float timePassed)
    {
        var prev = Random.state;        // save global RNG state
        Random.InitState((int)_controller.TimeManager.Tick);         // seed Unity's RNG

        for (int i = 0; i < pelletCount; i++)
        {
            float ang = aimDir + Random.Range(-15f, 15f);
            Vector2 dir = new(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
            var go = Instantiate(projectilePrefab);
            go.GetComponent<Projectile>().Init(projectileDef, Projectile.Role.Visual, muzzle.position, dir, projectileDef.speed + Random.Range(-speedVariance, speedVariance), 0);
        }

        Random.state = prev;
    }



    //NOTE:: TODO:: GET RID OF ALL THIS SHIT BELOW HERE





    public void Server_Fire(Vector2 muzzlePos, float baseAngleDeg, int pelletCount, float spreadDeg, float speed, float life, int seed, uint fireTick)
    {
        Debug.Log($"[Shotgun] Server_Fire called. Spawning {pelletCount} pellets");
        // How much time passed between owner press and server receive?
        float dt = (float)InstanceFinder.TimeManager.TickDelta;
        float secondsPassed = Mathf.Max(0f, (InstanceFinder.TimeManager.Tick - fireTick) * dt); //NOTE:: could this cause issues if dt varies tick to tick??

        // Smooth clamps (tweak to taste)
        float passedForServer = Mathf.Min(secondsPassed, projectileDef.maxPassedTime) * 0.5f; // conservative
        float passedForSpectators = Mathf.Min(secondsPassed, projectileDef.maxPassedTime);        // full (clamped)

        // Server-only pellets (authoritative hits)
        ServerSpawnBurst(muzzlePos, baseAngleDeg, pelletCount, spreadDeg, speed, life, seed, passedForServer);

        // Tell spectators to spawn visuals (exclude owner)
        Obs_Fire(muzzlePos, baseAngleDeg, pelletCount, spreadDeg, speed, life, seed, passedForSpectators);
    }


    public void Obs_Fire(Vector2 muzzlePos, float baseAngleDeg, int pelletCount, float spreadDeg, float speed, float life, int seed, float passedTimeSeconds)
    {

        Debug.Log($"[Shotgun] Obs_Fire called. Spawning {pelletCount} pellets");
        LocalSpawnBurst(muzzlePos, baseAngleDeg, pelletCount, spreadDeg, speed, life, seed, passedTimeSeconds);
    }

  

    // Deterministic spread: everyone gets the same offsets from the same seed
    public static float[] BuildOffsets(int pellets, float spreadDeg, int seed)
    {
        float half = spreadDeg * 0.5f;
        Random.InitState(seed);
        float[] arr = new float[pellets];

        for (int i = 0; i < pellets; i++)
        {
            // Simple uniform in [-half, half]
            float t = Random.Range(0, 1);
            arr[i] = (t * 2f - 1f) * half;
        } 
        return arr;
    }

    // Spawns a local visual burst (owner or spectators). No networking.
    private void LocalSpawnBurst(Vector2 muzzlePos, float baseAngleDeg, int pelletCount, float spreadDeg, float speed, float life, int seed, float passedTimeSeconds)
    {
        Debug.Log($"[Shotgun] LocalSpawnBurst called. Spawning {pelletCount} pellets");
        var offsets = BuildOffsets(pelletCount, spreadDeg, seed);
        for (int i = 0; i < pelletCount; i++)
        {
            float ang = baseAngleDeg + offsets[i];
            Vector2 dir = new(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

            var go = Instantiate(projectilePrefab);
            go.GetComponent<Projectile>().Init(projectileDef, Projectile.Role.Visual, muzzlePos, dir, speed, passedTimeSeconds);
        }
    }

    private void ServerSpawnBurst(Vector2 muzzlePos, float baseAngleDeg, int pelletCount,
                              float spreadDeg, float speed, float life,
                              int seed, float passedTimeSeconds)
    {
        var offsets = BuildOffsets(pelletCount, spreadDeg, seed);
        for (int i = 0; i < pelletCount; i++)
        {
            float ang = baseAngleDeg + offsets[i];
            Vector2 dir = new(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

            var go = Instantiate(projectilePrefab);
            var proj = go.GetComponent<Projectile>();
            proj.Init(projectileDef, Projectile.Role.Server, muzzlePos, dir, speed, passedTimeSeconds);

            // Optional: ignore the shooter's collider if your controller exposes one
            // var myCol = go.GetComponent<Collider2D>();
            // var ignore = _controller?.ShooterCollider;
            // if (myCol && ignore) Physics2D.IgnoreCollision(myCol, ignore, true);
        }
    }
}
