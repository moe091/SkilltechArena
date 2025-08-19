using UnityEngine;

public interface IWeapon
{
    void Initialize(WeaponController controller);
    Vector2 TryFire(int tick, Vector2 aimDir, ref Vector2 currentVel);
}