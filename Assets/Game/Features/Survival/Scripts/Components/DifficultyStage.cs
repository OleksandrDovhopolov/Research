using Unity.Entities;

namespace Survival
{
    // One row in the difficulty curve. Stored in a DynamicBuffer on the
    // DifficultyState entity. Sorted by TimeThreshold ascending — the
    // DifficultyProgressionSystem walks forward only, never sorts.
    public struct DifficultyStage : IBufferElementData
    {
        public float TimeThreshold;           // seconds since game start
        public float HpMultiplier;            // multiplies baked Enemy Health.Value
        public float DamageMultiplier;        // multiplies baked ContactDamage.DamagePerHit
        public float SpawnIntervalMultiplier; // 1.0 = baseline, 0.5 = twice as often
        public int CountPerWaveAddend;        // adds to baked SpawnConfig.CountPerWave
    }
}
