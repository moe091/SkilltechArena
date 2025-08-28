// ProjectileDef.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Projectiles/ProjectileDef")]
public class ProjectileDef : ScriptableObject
{
    [Header("Visuals")]
    public GameObject prefab;
    public float lifetime = 2.5f;  

    [Header("Motion")]
    public float speed = 24f;
    public float gravity = 0f;        // 0 for pellets, >0 for fireballs/rockets style arc

    [Header("Catch-up (Option A)")]
    public float maxPassedTime = 0.15f; // seconds cap
    public float catchupRate = 0.10f;   // fraction of a second consumed per rendered second

    [Header("Hits")]
    public bool despawnOnImpact = true;
    public int damage = 3;
    public LayerMask hitMask = ~0;

    // Optional: a short ID if you want to reference this type over RPCs
    public string id = "pellet"; // e.g., "pellet", "rocket", "fireball"
}
