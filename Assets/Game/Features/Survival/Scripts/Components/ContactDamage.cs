using Unity.Entities;

namespace Survival
{
    // Per-enemy contact damage config + cooldown timer. Used by
    // EnemyContactDamageSystem to drain the player's Health on contact.
    public struct ContactDamage : IComponentData
    {
        public float DamagePerHit;
        public float Interval;
        public float Timer;
        public float Radius;
    }
}
