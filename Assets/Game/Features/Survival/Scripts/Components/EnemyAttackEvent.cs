using Unity.Entities;

namespace Survival
{
    // One-shot event entity emitted whenever an enemy lands a contact hit.
    // EnemyVisualPoolManager consumes (and destroys) these each frame to
    // trigger the Attack animation on the attacker's visual.
    public struct EnemyAttackEvent : IComponentData
    {
        public Entity Attacker;
    }
}
