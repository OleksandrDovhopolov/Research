using Unity.Entities;

namespace Survival
{
    // Tuning parameters for enemy spawning. EnemySpawnSystem only reads this —
    // a future difficulty system can write it to ramp spawns up over time.
    public struct SpawnConfig : IComponentData
    {
        public float InitialDelay;
        public float Interval;
        public int CountPerWave;
        public float SpawnRadius;
    }
}
