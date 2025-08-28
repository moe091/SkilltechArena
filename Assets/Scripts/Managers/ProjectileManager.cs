using FishNet.Object;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.ObjectPooling.Examples;
using System;
using System.Collections.Generic;
//using UnityEditor.PackageManager;
using UnityEngine;

public class ProjectileManager : NetworkBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField] private List<ProjectileDef> projectileDefs = new();
    private Dictionary<string, ProjectileDef> _defsById = new();

    [SerializeField] private List<GameObject> projectilePrefabs = new();
    private Dictionary<string, GameObject> _prefabsById = new();

    private static Dictionary<ulong, Projectile> projectiles = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);

        BuildLookup();
    }

    private void BuildLookup()
    {
        _defsById.Clear();
        foreach(GameObject prefab in projectilePrefabs)
        {
            _prefabsById.Add(prefab.name, prefab);
            Debug.Log("ADDED PREFAB " +  prefab.name);
        }

        // Option A: auto-load all defs from a Resources folder (if you put them there)
        ProjectileDef[] loaded = Resources.LoadAll<ProjectileDef>("");
        foreach (var def in loaded)
        {
            if (def != null && !_defsById.ContainsKey(def.id))
                _defsById.Add(def.id, def);
        }

        // Option B: also include anything you dragged into the inspector
        foreach (var def in projectileDefs)
        {
            if (def != null && !_defsById.ContainsKey(def.id))
                _defsById.Add(def.id, def);
        }
    }



    public void SpawnProjectile(string defName, Vector2 pos, Vector2 dir, float speed, float timePassed, Projectile.Role role, ulong id, Collider2D shooterCollider)
    {
        ProjectileDef def = GetDef(defName);
        GameObject go = Instantiate(def.prefab);

        Projectile proj = go.GetComponent<Projectile>();
        proj.Init(null, role, pos, dir, speed, timePassed, id);

        // Ignore collision with the shooter
        if (shooterCollider != null)
        {
            Collider2D projCol = go.GetComponent<Collider2D>();
            if (projCol != null)
                Physics2D.IgnoreCollision(projCol, shooterCollider, true);
        }

        projectiles[id] = proj;
    }

    public void SpawnProj(GameObject prefab, Vector2 pos, Vector2 dir, int damage, float speed, float timePassed, Projectile.Role role, ulong id, Collider2D shooterCollider)
    {
        GameObject go = Instantiate(prefab);

        Projectile proj = go.GetComponent<Projectile>();
        proj.Init(null, role, pos, dir, speed, timePassed, id);

        // Ignore collision with the shooter
        if (shooterCollider != null)
        {
            Collider2D projCol = go.GetComponent<Collider2D>();
            if (projCol != null)
                Physics2D.IgnoreCollision(projCol, shooterCollider, true);
        }

        projectiles[id] = proj;
    }

    public void SpawnProj(string prefabName, Vector2 pos, Vector2 dir, int damage, float speed, float timePassed, Projectile.Role role, ulong id, Collider2D shooterCollider)
    {
        Debug.Log("SPAWNING " + prefabName);
        GameObject go = Instantiate(GetPrefab(prefabName));

        Projectile proj = go.GetComponent<Projectile>();
        proj.Init(null, role, pos, dir, speed, timePassed, id);
        proj.SetDamage(damage);

        // Ignore collision with the shooter
        if (shooterCollider != null)
        {
            Collider2D projCol = go.GetComponent<Collider2D>();
            if (projCol != null)
                Physics2D.IgnoreCollision(projCol, shooterCollider, true);
        }

        projectiles[id] = proj;
    }





    [ObserversRpc(ExcludeOwner = false, RunLocally = false)]
    private void CollisionRPC(ulong id)
    {
        var proj = GetProjectile(id);
        if (proj != null)
        {
            Debug.Log("proj " + id + " found. Destroying");
            Destroy(proj.gameObject); // client visual
        } else
        {
            //This is fine, most projectiles will be destroyed client-side before the RPC arrives.
        }
    }


    public static void ProjectileCollision(Projectile projectile, Collider2D other)
    {
        Debug.Log("Projectile Collision with: " + other.name);
        if (other.TryGetComponent<IHittable>(out var hittable))
        {
            hittable.OnHit(projectile, projectile.damage);
        }

        if (projectile._def.despawnOnImpact)
        {
            ulong id = projectile.id;
            Destroy(projectile.gameObject);      // authoritative server projectile
            if (Instance.IsServerStarted)
            {
                Instance.CollisionRPC(id);             // tell clients to kill their visual in case they missed the collision
            }
        }
        else
        {
            Debug.Log("Projectile Collision, no DespawnOnImpact! id=" + projectile.id);
        }
    }









    //****************************** GETTERS AND STUFF BELOW HERE ***********************************\\



    public static ulong GetUniqueHash(int shooterId, uint tickId, int pelletId)
    {
        // Make sure each part fits into the space you allocate
        // - shooterId: 16 bits (FishNet OwnerId is a ushort)
        // - tickId:    32 bits
        // - pelletId:  16 bits

        ulong sid = (ulong)(ushort)shooterId;  // clamp to 16 bits
        ulong tid = (ulong)(uint)tickId;       // clamp to 32 bits
        ulong pid = (ulong)(ushort)pelletId;   // clamp to 16 bits

        return (sid << 48) | (tid << 16) | pid;
    }


    public ProjectileDef GetDef(string id)
    {
        _defsById.TryGetValue(id, out var def);
        return def;
    }

    public GameObject GetPrefab(string id)
    {
        _prefabsById.TryGetValue(id, out var prefab);
        return prefab;
    }

    public Projectile GetProjectile(ulong id)
    {
        projectiles.TryGetValue(id, out var proj);
        return proj; // null if not found
    }




    public void UnregisterProjectile(ulong id)
    {
        projectiles.Remove(id);
    }
}
