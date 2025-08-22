using UnityEngine;

public interface IWeapon
{
    void Initialize(WeaponController controller);
    Vector2 TryFire(int tick, Vector2 aimDir, bool isReplayed, ref Vector2 currentVel);

    public void SpawnProjectiles(Vector2 pos, float aimDir, float timePassed);

    public void Server_Fire(Vector2 muzzlePos, float baseAngleDeg, int pelletCount, float spreadDeg, float speed, float life, int seed, uint fireTick);

    public void Obs_Fire(Vector2 muzzlePos, float baseAngleDeg, int pelletCount, float spreadDeg, float speed, float life, int seed, float passedTimeSeconds);
}