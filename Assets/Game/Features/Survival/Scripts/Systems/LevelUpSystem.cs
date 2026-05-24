using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PickupXpSystem))]
    [BurstCompile]
    public partial struct LevelUpSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Level>();
            state.RequireForUpdate<Experience>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Main-thread foreach — one player, the WithNone filter blocks
            // re-entry while a level-up is being processed.
            foreach (var (level, exp, entity) in
                SystemAPI.Query<RefRW<Level>, RefRO<Experience>>()
                    .WithAll<PlayerTag>()
                    .WithNone<LevelUpRequest, PendingUpgrade>()
                    .WithEntityAccess())
            {
                if (exp.ValueRO.Current < level.ValueRO.NextThresholdXp)
                    continue;

                level.ValueRW.Value++;
                level.ValueRW.NextThresholdXp = NextThreshold(level.ValueRO.Value);
                ecb.AddComponent<LevelUpRequest>(entity);
            }
        }

        // Curve: 10, 15, 23, 34, 51, … (each level needs ~1.5× the previous).
        private static int NextThreshold(int level)
        {
            return (int)math.round(10f * math.pow(1.5f, level - 1));
        }
    }
}
