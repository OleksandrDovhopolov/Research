using Unity.Entities;

namespace Survival
{
    // One-shot event emitted by AutoShootSystem whenever the player fires a
    // projectile. PlayerVisualSync consumes (and destroys) these each frame
    // to trigger the bow Shoot animation on the visual companion.
    public struct PlayerShotEvent : IComponentData
    {
    }
}
