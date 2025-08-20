using FishNet;
using FishNet.Managing.Predicting;
using FishNet.Object;           // to access controller.ServerManager, controller.NetworkObject
using UnityEngine;

public class Shotgun : MonoBehaviour, IWeapon
{
    [Header("Setup")]
    [SerializeField] private Bullet projectilePrefab;  // assign your Projectile2D prefab
    [SerializeField] private Transform muzzle;               // optional; otherwise we’ll use transform.position

    [Header("Ballistics")]
    [SerializeField] private float projectileSpeed = 24f;    // units/sec (2D)
    [SerializeField] private int pellets = 8;                // number of pellets per shot
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
    public Vector2 TryFire(int tick, Vector2 aimDir, ref Vector2 currentVel) 
    {
        if (_controller == null)
        {
            Debug.LogError("[Shotgun] TryFire failed - _controller is null");
        }

        // OWNER FEEL (optional): play muzzle flash / sound locally
        if (_controller.IsOwner)
        {
            // TODO: play local VFX/SFX here
        }

        // SERVER: spawn pellets
        
        
        
        //if (tick - lastShotTick > cooldown && (_controller.IsServerInitialized || _controller.IsOwner))
        if (_controller.IsServerInitialized || _controller.IsOwner)
        {
            if (_controller.IsServerInitialized)
                Server_SpawnPellets(aimDir.normalized);
            
            Vector2 recoilVector = aimDir.normalized * -recoilForce;
            float yRecoil = recoilVector.y;
            float xRecoil = recoilVector.x;

            float yDamper = currentVel.y * -0.5f;
            yRecoil = yRecoil + yDamper;

            currentVel += new Vector2(xRecoil, yRecoil);

            if (shootSfx != null && _audioSource != null && _lastShotTick != tick)
                _audioSource.PlayOneShot(shootSfx);

            if (_animator != null && _lastShotTick != tick)
            {
                _animator.ResetTrigger("Shoot");
                _animator.SetTrigger("Shoot");
            }
            else
            {
                Debug.LogWarning("[Shotgun] Animator component is null, can't play shoot animation!");
            }

            _lastShotTick = tick;

        } else
        {

        }

            return (aimDir.normalized * -recoilForce);

    }

    private void Server_SpawnPellets(Vector2 baseDir)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[Shotgun] Unable to SpawnProjectile - projectilePrefab is null");
            return;
        }

        // Spawn position at muzzle (optionally nudge back along aim to avoid self-overlap)
        Vector2 muzzlePos = muzzle ? (Vector2)muzzle.position : (Vector2)transform.position;
        if (muzzleBackOffset > 0f)
            muzzlePos -= baseDir * muzzleBackOffset;

        int count = Mathf.Max(1, pellets);
        float half = spreadDegrees * 0.5f;

        // Simple MVP random spread (you can switch to deterministic later)
        for (int i = 0; i < count; i++)
        {
            float offset = Random.Range(-half, half);
            Vector2 dir = Quaternion.Euler(0f, 0f, offset) * baseDir;
            Vector2 vel = dir * (projectileSpeed + Random.Range(-speedVariance, speedVariance));

            // Instantiate and spawn
            var proj = Instantiate(projectilePrefab);
            _controller.ServerManager.Spawn(proj.NetworkObject);

            // Ignore the shooter’s collider if provided on the controller
            var ignoreCol = _controller.ShooterCollider;

            // Initialize projectile (server-only)
            proj.Init(_controller.NetworkObject, muzzlePos, vel, ignoreCol);
        }
    }

}
