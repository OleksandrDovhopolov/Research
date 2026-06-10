using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    // XZ-plane rectangle the player's position is clamped to.
    // float2: .x = world X, .y = world Z.
    public struct ArenaBounds : IComponentData
    {
        public float2 Min;
        public float2 Max;
    }
}
