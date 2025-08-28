// Projectile2D.cs
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum Role { Visual, Server }   // Visual: owner/spectator; Server: authoritative

    [SerializeField] public ProjectileDef _def;
    Role _role;
    Vector2 _vel;
    float _life;
    float _catchupLeft; // seconds to consume smoothly
    public ulong id;
    [SerializeField] public int damage = 1;

    SpriteRenderer _sr;
    Collider2D _col;



    public void Init(ProjectileDef def, Role role, Vector2 pos, Vector2 dir, float speedOverride, float passedTimeSec, ulong projectileId)
    {
        if (def != null)
        {
            _def = def;
        }
        _role = role;
        id = projectileId;

        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_col == null) _col = GetComponent<Collider2D>();

        //if (_sr) _sr.sprite = def.sprite;
        //if (_col) _col.enabled = (role == Role.Server);   // clients don't collide; server does
        if (_col) _col.enabled = true; //NOTE:: FOR NOW I AM COLLIDING ON SERVER AND CLIENT. ONLY SERVER WILL APPLY DAMANGE, BUT CLIENTS WILL DESTROY PROJECTILE IMMEDIATELY

        transform.position = pos;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        float spd = (speedOverride > 0f) ? speedOverride : def.speed;
        _vel = dir.normalized * spd;

        _life = _def.lifetime; 

        // Clamp and store catch-up to consume over a few frames
        _catchupLeft = Mathf.Clamp(passedTimeSec, 0f, _def.maxPassedTime);
    }

    void Update()
    {
        // Smooth catch-up: consume a small portion of already-passed time
        float extra = 0f;
        if (_catchupLeft > 0f)
        {
            float consume = _def.catchupRate * Time.deltaTime;
            extra = Mathf.Min(_catchupLeft, consume);
            //Debug.Log($"Projectile catching up! catchupLeft={_catchupLeft}  -  extra={extra} ");
            _catchupLeft -= extra;
        }

        float dt = Time.deltaTime + extra;

        // Basic kinematics (gravity optional via def)
        _vel += new Vector2(0f, -_def.gravity) * dt;
        transform.position += (Vector3)(_vel * dt);

        _life -= Time.deltaTime;
        if (_life <= 0f) Destroy(gameObject);
    }

    // Server-only hits
    void OnTriggerEnter2D(Collider2D other)
    {
        ProjectileManager.ProjectileCollision(this, other);
    }

    void OnDestroy()
    {
        if (ProjectileManager.Instance != null)
            ProjectileManager.Instance.UnregisterProjectile(id);
    }

    public void SetDamage(int dmg)
    {
        damage = dmg;
    }
}
