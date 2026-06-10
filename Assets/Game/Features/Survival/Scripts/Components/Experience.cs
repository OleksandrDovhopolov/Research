using Unity.Entities;

namespace Survival
{
    // Player's accumulated XP. Level-up will read/write this on Day 6.
    public struct Experience : IComponentData
    {
        public int Current;
    }
}
