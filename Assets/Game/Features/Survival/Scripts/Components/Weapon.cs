using Unity.Entities;

namespace Survival
{
    public struct Weapon : IComponentData
    {
        public float FireInterval;
        public float FireTimer;
        public float ProjectileSpeed;
        public float ProjectileDamage;
        public float ProjectileLifetime;
        // World-units выше PlayerPosition.Y, где появляется снаряд. Игрок
        // визуально масштабирован — пуля иначе вылетает из ступней.
        public float MuzzleHeight;
        public Entity ProjectilePrefab;
    }
}
