
public interface IHittable
{
    // Server-side only: apply damage/effects
    void OnHit(Projectile proj, int dmg);
}