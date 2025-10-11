using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : NetworkBehaviour
{
    public readonly SyncVar<int> explodeTick = new SyncVar<int>();
    private bool _exploded;
    private GameObject _explosionPrefab;

    [SerializeField] private LayerMask damageMask;
    [SerializeField] private LayerMask losBlockMask;
    public float explodeRadius = 5f;
    public float maxDamage = 50f;
    private static readonly Collider2D[] _hits = new Collider2D[32];

    public override void OnStartClient()
    {
        _explosionPrefab = Resources.Load<GameObject>("FX/weapons/ExplosionFX");
        if (_explosionPrefab == null)
            Debug.LogWarning("[Grenade] Could not load ExplosionFX prefab from Resources!");
    }
    public override void OnStartNetwork()
    {
        TimeManager.OnTick += HandleTick;
    }

    public override void OnStopNetwork()
    {
        TimeManager.OnTick -= HandleTick;
    }

    private void HandleTick()
    {
        if (_exploded)
            return;

        if (TimeManager.Tick >= explodeTick.Value)
        {
            _exploded = true;

            if (IsClientInitialized)
                ExplodeClient();

            if (IsServerStarted)
                ExplodeServer();
        }
    }

    private void ExplodeServer()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, explodeRadius, _hits, damageMask);
        Debug.Log("[Grenade.ExplodeServer] Exploded, collision count = " + count);

        for (int i = 0; i < count; i++)
        {
            Collider2D player = _hits[i];

            if (losBlockMask.value != 0)
            {
                Vector2 targetPoint = player.bounds.ClosestPoint(transform.position);
                var hit = Physics2D.Linecast(transform.position, targetPoint, losBlockMask);
                if (hit.collider != null)
                {
                    Debug.Log("[Grenade.ExplodeServer] Collision blocked by wall! Skipping!");
                    Debug.Log("[Grenade.ExplodeServer] Collision blocked by wall! Skipping!");
                    Debug.Log("[Grenade.ExplodeServer] Collision blocked by wall! Skipping!");
                    continue; // blocked by wall/obstacle
                }
            }

            float dist = Vector2.Distance(transform.position, player.bounds.ClosestPoint(transform.position));
            float t = Mathf.Clamp01(dist / explodeRadius);
            float damage = Mathf.Lerp(maxDamage, maxDamage / 5, t);

            Debug.Log($"[Grenade.ExplodeServer] dist={dist}, applying {damage} damage!");
            PlayerController pc = player.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                Debug.Log("Applying damage: " + (int)-damage);
                pc.ChangeHealth((int)-damage);
            }

        }

        Despawn(gameObject);
        // TODO:
        // - Overlap for targets, apply damage/impulse
        // - Optionally: ObserversRpc to force-start FX on all clients
        // - Finally: Despawn(gameObject);
    }

    private void ExplodeClient()
    {
        if (_explosionPrefab != null)
        {
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        } else
        {
            Debug.LogWarning("[Grenade] Explosion prefab missing!");
        }

        var sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
            sprite.enabled = false;
    }
}
