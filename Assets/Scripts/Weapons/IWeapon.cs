using UnityEngine;

public interface IWeapon
{
    void Initialize(WeaponController controller);
    Vector2 TryFire(int tick, Vector2 aimDir, bool isReplayed, ref Vector2 currentVel);

    public void SpawnProjectiles(Vector2 pos, float aimDir, float timePassed, bool isAuthority, uint fireTick);
}