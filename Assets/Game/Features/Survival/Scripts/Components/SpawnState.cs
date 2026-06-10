using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    // Runtime state of the spawner: countdown to the next wave + RNG.
    public struct SpawnState : IComponentData
    {
        public float Timer;
        public Random Random;
        // Latches true after the one-shot initial burst (if configured) has
        // been spawned, so subsequent OnUpdate ticks skip it.
        public bool InitialBurstDone;
    }
}
