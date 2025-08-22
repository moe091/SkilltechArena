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

    SpriteRenderer _sr;
    Collider2D _col;

    public void Init(ProjectileDef def, Role role, Vector2 pos, Vector2 dir, float speedOverride, float passedTimeSec)
    {
        _def = def;
        _role = role;

        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        if (_col == null) _col = GetComponent<Collider2D>();

        if (_sr) _sr.sprite = def.sprite;
        if (_col) _col.enabled = (role == Role.Server);   // clients don't collide; server does

        transform.position = pos;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        float spd = (speedOverride > 0f) ? speedOverride : def.speed;
        _vel = dir.normalized * spd;

        _life = def.lifetime;

        // Clamp and store catch-up to consume over a few frames
        _catchupLeft = Mathf.Clamp(passedTimeSec, 0f, def.maxPassedTime);
    }

    void Update()
    {
        // Smooth catch-up: consume a small portion of already-passed time
        float extra = 0f;
        if (_catchupLeft > 0f)
        {
            float consume = _def.catchupRate * Time.deltaTime;
            extra = Mathf.Min(_catchupLeft, consume);
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
        if (_role != Role.Server || !_col) return;

        if (((1 << other.gameObject.layer) & _def.hitMask) == 0)
            return;

        // TODO: damage here (server only)
        if (_def.despawnOnImpact) Destroy(gameObject);
    }
}
