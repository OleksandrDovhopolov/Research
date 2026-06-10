using Unity.Entities;

namespace Survival
{
    // Singleton holding the elapsed survival time and the cached multipliers
    // of the currently-active DifficultyStage. EnemySpawnSystem reads from
    // the cached fields each frame so the spawn job never walks the buffer.
    public struct DifficultyState : IComponentData
    {
        public float ElapsedTime;
        public int CurrentStageIndex;

        // Cached from the active stage.
        public float HpMultiplier;
        public float DamageMultiplier;
        public float SpawnIntervalMultiplier;
        public int CountPerWaveAddend;
    }
}
