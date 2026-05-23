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
        public Entity ProjectilePrefab;
    }
}
