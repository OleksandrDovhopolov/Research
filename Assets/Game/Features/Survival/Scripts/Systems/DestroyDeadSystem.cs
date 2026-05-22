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

            new DestroyDeadJob
            {
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(DeadTag))]
    public partial struct DestroyDeadJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        // Per-entity DestroyEntity unrolls each entity's LinkedEntityGroup,
        // so child mesh entities are destroyed together with the enemy root.
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity)
        {
            Ecb.DestroyEntity(chunkIndex, entity);
        }
    }
}
