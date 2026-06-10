using Unity.Entities;

namespace Survival
{
    public struct Level : IComponentData
    {
        public int Value;
        public int NextThresholdXp;
    }
}
