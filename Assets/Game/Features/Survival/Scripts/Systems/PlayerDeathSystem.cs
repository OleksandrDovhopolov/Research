using Unity.Burst;
using Unity.Entities;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyContactDamageSystem))]
    [BurstCompile]
    public partial struct PlayerDeathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Adds PlayerDeadTag exactly once. We do NOT destroy the player here
            // — GameOverBridge will pause the game and trigger a scene reload.
            foreach (var (h, entity) in
                SystemAPI.Query<RefRO<Health>>()
                    .WithAll<PlayerTag>()
                    .WithNone<PlayerDeadTag>()
                    .WithEntityAccess())
            {
                if (h.ValueRO.Value <= 0f)
                    ecb.AddComponent<PlayerDeadTag>(entity);
            }
        }
    }
}
