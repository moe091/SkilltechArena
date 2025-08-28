using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IHittable
{
    private PlayerPrediction _prediction;
    private WeaponController _weaponController;

    [SerializeField] private HealthBar _healthBar;
    [SerializeField] private int maxHealth = 100;
    private readonly SyncVar<int> _health = new();

    void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
        _prediction = GetComponent<PlayerPrediction>();

        _healthBar.SetMaxHealth(maxHealth);
        _healthBar.SetHealth(maxHealth);

        _health.OnChange += OnHealthChanged;
    }

    public override void OnStartServer()
    {
        _health.Value = maxHealth; // server initializes
    }


    private void Start()
    {

    }

    void FixedUpdate()
    {

    }


    public void OnHit(Projectile proj, int dmg)
    {
        if (!IsServerInitialized) return;

        ChangeHealth(-dmg);
    }


    public void ChangeHealth(int amount)
    {
        _health.Value = _health.Value + amount;
        _healthBar.SetHealth(_health.Value);

        if (_health.Value <= 0)
        {
            Die();
        }
    }

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        // If this HealthBar is worldspace above the player, update for everyone:
        if (_healthBar != null)
            _healthBar.SetHealth(next);

        // If you also have a screen-space HUD for the local player:
        // if (IsOwner) HUD.Instance.SetHealth(newV / maxHealth);
    }

    private void Die()
    {
        Debug.Log("Player has become Died!");
    }




}
