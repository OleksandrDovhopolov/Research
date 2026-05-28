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

        // Curve: 8, 10, 14, 18, 23, 30, 39, 51, 66, 86, … (each level needs
        // ~1.3× the previous). Slower growth than before so the player sees
        // more of the 7-upgrade pool during a typical 5-min run.
        private static int NextThreshold(int level)
        {
            return (int)math.round(8f * math.pow(1.3f, level - 1));
        }
    }
}
