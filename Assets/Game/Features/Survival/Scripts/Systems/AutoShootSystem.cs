using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerPositionSystem))]
    [BurstCompile]
    public partial struct AutoShootSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Weapon>();
            state.RequireForUpdate<PlayerPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var weapon = ref SystemAPI.GetSingletonRW<Weapon>().ValueRW;
            weapon.FireTimer -= SystemAPI.Time.DeltaTime;
            if (weapon.FireTimer > 0f)
                return;

            float3 playerPos = SystemAPI.GetSingleton<PlayerPosition>().Value;

            // Nearest-enemy scan.
            float bestDistSq = float.MaxValue;
            float3 targetPos = float3.zero;
            bool found = false;
            foreach (var transform in
                SystemAPI.Query<RefRO<LocalTransform>>().WithAll<EnemyTag>().WithNone<DeadTag>())
            {
                float distSq = math.distancesq(transform.ValueRO.Position.xz, playerPos.xz);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    targetPos = transform.ValueRO.Position;
                    found = true;
                }
            }

            if (!found)
            {
                // Hold at "ready" — without this the timer keeps going negative
                // while idle and the first enemy triggers a burst to catch up.
                weapon.FireTimer = 0f;
                return;
            }

            float3 toTarget = targetPos - playerPos;
            toTarget.y = 0f;
            if (math.lengthsq(toTarget) <= 1e-6f)
                return;

            weapon.FireTimer += weapon.FireInterval;

            float3 direction = math.normalize(toTarget);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            Entity prefab = weapon.ProjectilePrefab;
            Entity projectile = ecb.Instantiate(prefab);

            LocalTransform transformValue = SystemAPI.GetComponent<LocalTransform>(prefab);
            float3 spawnPos = playerPos;
            spawnPos.y += weapon.MuzzleHeight;
            transformValue.Position = spawnPos;

            ecb.SetComponent(projectile, transformValue);
            ecb.SetComponent(projectile, new MoveDirection { Value = direction });
            ecb.SetComponent(projectile, new MoveSpeed { Value = weapon.ProjectileSpeed });
            ecb.SetComponent(projectile, new Damage { Value = weapon.ProjectileDamage });
            ecb.SetComponent(projectile, new Lifetime { Value = weapon.ProjectileLifetime });
        }
    }
}
