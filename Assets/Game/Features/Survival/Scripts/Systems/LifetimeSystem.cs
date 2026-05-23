using Unity.Burst;
using Unity.Entities;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct LifetimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Lifetime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new LifetimeJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithNone(typeof(DeadTag))]
    public partial struct LifetimeJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
            ref Lifetime lifetime)
        {
            lifetime.Value -= DeltaTime;
            if (lifetime.Value <= 0f)
                Ecb.AddComponent<DeadTag>(chunkIndex, entity);
        }
    }
}
