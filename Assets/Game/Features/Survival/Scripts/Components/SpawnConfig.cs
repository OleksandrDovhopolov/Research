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
        // Per-spawn random multiplier applied to each enemy's baked MoveSpeed.
        // Typical: Min=0.7, Max=1.3 → толпа «расслаивается» по скорости.
        public float MoveSpeedMin;
        public float MoveSpeedMax;
        // One-shot burst spawned at game start (helpful for stress-testing /
        // Burst-benchmarking). 0 = disabled. After spawn, SpawnState marks
        // InitialBurstDone = true and normal waves continue as usual.
        public int InitialBurstCount;
    }
}
