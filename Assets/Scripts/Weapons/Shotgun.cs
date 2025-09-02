using FishNet;
using FishNet.Managing.Predicting;
using FishNet.Managing.Timing;
using FishNet.Object;           // to access controller.ServerManager, controller.NetworkObject
using UnityEngine;
using UnityEngine.UIElements;

public class Shotgun : MonoBehaviour, IWeapon
{ 
    [Header("Setup")]
    // Option A (no NetworkObject): one generic prefab + data asset
    [SerializeField] private GameObject projectilePrefab;   // the visual/server prefab (no NetworkObject)

    [SerializeField] private Transform muzzle;               // optional; otherwise we’ll use transform.position

    [Header("Ballistics")]
    [SerializeField] private float projectileSpeed = 24f;    // units/sec (2D)
    [SerializeField] private int damage = 5;    // units/sec (2D)
    [SerializeField] private int pelletCount = 1;                // number of pellets per shot
    [SerializeField] private float spreadDegrees = 10f;      // full cone width (e.g., 12° => ±6°)
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
            uint fireTick = _controller.TimeManager.Tick;
            SpawnProjectiles(muzzlePos, baseAngleDeg, 0, _controller.IsServerInitialized, fireTick); //always spawn local projectiles

            if (!_controller.IsServerInitialized) //if we aren't server, send RPC to spawn projectiles on server
                _controller.ServerFire(muzzlePos, baseAngleDeg, fireTick); //grab the tick HERE because serverFire is an RPC, we want to pass the tick from client
            else
                _controller.ObserverFire(muzzlePos, baseAngleDeg, fireTick);
        }
        if (!isReplayed)
        {
            Debug.Log("TryFire playing animation");
            _audioSource.Play();
            _animator.SetTrigger("Shoot");
        }

        Vector2 recoilVector = aimDir.normalized * -recoilForce;
        float yRecoil = recoilVector.y;
        float xRecoil = recoilVector.x;

        float yDamper = currentVel.y * -0.5f;
        yRecoil = yRecoil + yDamper;

        currentVel += new Vector2(xRecoil, yRecoil);

        return (aimDir.normalized * -recoilForce);
    }




    public void SpawnProjectiles(Vector2 pos, float aimDir, float timePassed, bool isAuthority, uint fireTick)
    {
        var prev = Random.state;        // save global RNG state
        Random.InitState((int)fireTick);         // seed Unity's RNG
        Projectile.Role role = isAuthority ? Projectile.Role.Server : Projectile.Role.Visual;

        for (int i = 0; i < pelletCount; i++)
        {
            ulong id = ProjectileManager.GetUniqueHash(_controller.NetworkObject.OwnerId, fireTick, i);

            float ang = aimDir + Random.Range(-spreadDegrees, spreadDegrees);
            Vector2 dir = new(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)); 
            ProjectileManager.Instance.SpawnProj("Projectile", pos, dir, damage, projectileSpeed + Random.Range(-speedVariance, speedVariance), timePassed, role, id, _controller.shooterCollider);
        }

        Random.state = prev;
    }

}
