using Unity.Entities;

namespace Survival
{
    // Pulls XP gems toward the player when they enter Radius. PickupRadius
    // (separate component) remains the inner "absorb" zone — gems flying in
    // get absorbed when they cross it.
    public struct XpMagnet : IComponentData
    {
        public float Radius;
        public float Speed;
    }
}
