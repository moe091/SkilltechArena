using UnityEngine;

public interface IWeapon
{
    void Initialize(WeaponController controller);
    void TryFire(int tick, Vector2 aimDir);
}