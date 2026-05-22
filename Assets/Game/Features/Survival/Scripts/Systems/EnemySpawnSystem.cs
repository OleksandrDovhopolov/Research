using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerPositionSystem))]
    [BurstCompile]
    public partial struct EnemySpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnState>();
            state.RequireForUpdate<PlayerPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<SpawnConfig>();
            var spawn = SystemAPI.GetSingletonRW<SpawnState>();

            spawn.ValueRW.Timer -= SystemAPI.Time.DeltaTime;
            if (spawn.ValueRW.Timer > 0f)
                return;

            spawn.ValueRW.Timer += config.Interval;

            var prefabs = SystemAPI.GetSingletonBuffer<EnemyPrefab>();
            if (prefabs.Length == 0)
                return;

            float3 playerPos = SystemAPI.GetSingleton<PlayerPosition>().Value;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            ref var rng = ref spawn.ValueRW.Random;
            for (int i = 0; i < config.CountPerWave; i++)
            {
                Entity prefab = prefabs[rng.NextInt(prefabs.Length)].Value;

                float angle = rng.NextFloat(0f, math.PI * 2f);
                float3 offset = new float3(math.cos(angle), 0f, math.sin(angle)) * config.SpawnRadius;

                // Copy the prefab's baked LocalTransform so authored scale/rotation survive.
                LocalTransform transform = SystemAPI.GetComponent<LocalTransform>(prefab);
                transform.Position = playerPos + offset;

                Entity enemy = ecb.Instantiate(prefab);
                ecb.SetComponent(enemy, transform);
            }
        }
    }
}
