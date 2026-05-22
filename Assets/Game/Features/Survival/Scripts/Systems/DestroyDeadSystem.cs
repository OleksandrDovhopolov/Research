using Unity.Burst;
using Unity.Entities;

namespace Survival
{
    // Permanent, condition-agnostic destroyer: removes every entity marked DeadTag.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct DestroyDeadSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DeadTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var query = SystemAPI.QueryBuilder().WithAll<DeadTag>().Build();
            ecb.DestroyEntity(query, EntityQueryCaptureMode.AtPlayback);
        }
    }
}
