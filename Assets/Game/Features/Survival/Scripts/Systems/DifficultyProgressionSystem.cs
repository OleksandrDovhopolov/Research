using Unity.Burst;
using Unity.Entities;

namespace Survival
{
    // Ticks elapsed survival time and advances the active DifficultyStage
    // whenever the elapsed time crosses the next threshold. Cached
    // multipliers on DifficultyState are read by EnemySpawnSystem.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemySpawnSystem))]
    [BurstCompile]
    public partial struct DifficultyProgressionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DifficultyState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var diffRW = SystemAPI.GetSingletonRW<DifficultyState>();
            ref var diff = ref diffRW.ValueRW;
            diff.ElapsedTime += SystemAPI.Time.DeltaTime;

            DynamicBuffer<DifficultyStage> stages = SystemAPI.GetSingletonBuffer<DifficultyStage>(true);
            if (stages.Length == 0)
                return;

            // Walk forward to the last stage whose threshold <= ElapsedTime.
            int target = diff.CurrentStageIndex;
            while (target + 1 < stages.Length && stages[target + 1].TimeThreshold <= diff.ElapsedTime)
                target++;

            if (target != diff.CurrentStageIndex && target >= 0)
            {
                diff.CurrentStageIndex = target;
                diff.HpMultiplier = stages[target].HpMultiplier;
                diff.DamageMultiplier = stages[target].DamageMultiplier;
                diff.SpawnIntervalMultiplier = stages[target].SpawnIntervalMultiplier;
                diff.CountPerWaveAddend = stages[target].CountPerWaveAddend;
            }
        }
    }
}
