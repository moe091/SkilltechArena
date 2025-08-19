using FishNet.Object;          // NetworkBehaviour, ServerManager
using UnityEngine;

/// <summary>
/// Server-authoritative 2D projectile.
/// - Server sets initial position/velocity and simulates via Rigidbody2D.
/// - Clients receive state via net replication (no client-side physics).
/// - Despawns by lifetime tick or on impact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : NetworkBehaviour
{
    [Header("Tuning")]
    [Tooltip("How long the projectile lives (seconds).")]
    [SerializeField] private float lifetimeSeconds = 2.5f;

    [Tooltip("Should the projectile despawn on the first trigger hit?")]
    [SerializeField] private bool despawnOnImpact = false;

    [Tooltip("Optional: hit layers (leave empty to let physics layer matrix decide).")]
    [SerializeField] private LayerMask hitMask = ~0;

    // Cached components
    private Rigidbody2D _rb;
    private Collider2D _col;

    // Who fired us (used to ignore collisions with the owner)
    private NetworkObject _owner;

    // Server-side despawn time, in ticks.
    private int _despawnTick;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        // Recommended RB2D defaults for net projectiles
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.None; // server drives state; clients interpolate via net
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _col.isTrigger = true; // hit detection via trigger by default
    }
    
    /// <summary>
    /// Initialize the projectile on the SERVER after spawning.
    /// </summary>
    /// <param name="owner">Shooter NetworkObject (to ignore friendly collision).</param>
    /// <param name="worldPosition">Starting world position.</param>
    /// <param name="initialVelocity">Initial RB2D velocity.</param>
    /// <param name="ignore">A collider on the shooter to ignore (e.g., main body collider).</param>
    [Server]
    public void Init(NetworkObject owner, Vector2 worldPosition, Vector2 initialVelocity, Collider2D ignore)
    {
        _owner = owner;

        transform.position = worldPosition;
        _rb.velocity = initialVelocity;

        // Lifetime → ticks
        int lifetimeTicks = Mathf.Max(1, Mathf.CeilToInt(lifetimeSeconds * (float)base.TimeManager.TickRate));
        _despawnTick = (int)(base.TimeManager.Tick + lifetimeTicks);

        // Ignore shooter collision (basic friendly-fire prevention)
        if (ignore != null && _col != null)
            Physics2D.IgnoreCollision(_col, ignore, true);
    }

    private void Update()
    {
        // Only the server decides when to despawn
        if (IsServer && base.TimeManager.Tick >= _despawnTick)
        {
            Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServerInitialized) return;

        // Ignore our owner
        if (_owner && other.GetComponentInParent<NetworkObject>() == _owner)
            return;

        // Optional: layer filter
        if (((1 << other.gameObject.layer) & hitMask) == 0)
            return;

        // TODO: Apply damage here (server-side), e.g.:
        // var damageable = other.GetComponentInParent<IDamageable>();
        // damageable?.TakeDamage(damageAmount, _owner);

        if (despawnOnImpact)
        {
            Despawn();
        }
    }

    [Server]
    private void Despawn()
    {
        if (base.IsSpawned)
            base.Despawn(); // returns to pool if using pooling; otherwise destroys
    }
}
