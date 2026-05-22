using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    // TEMPORARY (testing): marks an enemy dead when it nearly touches the player.
    // Day 5 replaces the trigger with HP-based death; DestroyDeadSystem stays.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMoveToPlayerSystem))]
    [BurstCompile]
    public partial struct EnemyTouchPlayerSystem : ISystem
    {
        // Tune to taste: enemy despawns when its center is this close to the player.
        private const float TouchDistance = 5f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerPosition>();
            state.RequireForUpdate<EnemyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new EnemyTouchPlayerJob
            {
                PlayerPos = SystemAPI.GetSingleton<PlayerPosition>().Value,
                TouchDistanceSq = TouchDistance * TouchDistance,
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(EnemyTag))]
    [WithNone(typeof(DeadTag))]
    public partial struct EnemyTouchPlayerJob : IJobEntity
    {
        public float3 PlayerPos;
        public float TouchDistanceSq;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
            in LocalTransform transform)
        {
            float2 delta = transform.Position.xz - PlayerPos.xz;
            if (math.lengthsq(delta) < TouchDistanceSq)
                Ecb.AddComponent<DeadTag>(chunkIndex, entity);
        }
    }
}
