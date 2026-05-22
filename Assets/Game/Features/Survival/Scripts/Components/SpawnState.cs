using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    // Runtime state of the spawner: countdown to the next wave + RNG.
    public struct SpawnState : IComponentData
    {
        public float Timer;
        public Random Random;
    }
}
