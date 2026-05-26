using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    // World-space XZ direction from the player to the nearest enemy. Written
    // every frame by AutoShootSystem (regardless of whether it's the firing
    // frame), read by PlayerVisualSync to face the visual at the target.
    //
    // Value is float3.zero when there is no enemy in scan range — the visual
    // falls back to ECS rotation (movement direction) in that case.
    public struct AimDirection : IComponentData
    {
        public float3 Value;
    }
}
