using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    public struct MoveDirection : IComponentData
    {
        public float3 Value;
    }
}
