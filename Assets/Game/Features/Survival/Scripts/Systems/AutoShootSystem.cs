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
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var weapon = ref SystemAPI.GetSingletonRW<Weapon>().ValueRW;
            weapon.FireTimer -= SystemAPI.Time.DeltaTime;

            float3 playerPos = SystemAPI.GetSingleton<PlayerPosition>().Value;
            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();

            // Nearest-enemy scan runs EVERY frame (not just on firing frame) —
            // PlayerVisualSync needs an up-to-date aim direction so the visual
            // stays pointed at the target while the fire timer ticks down.
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

            // Publish the aim direction (or zero if no target) so the visual
            // companion can decide whether to override its rotation.
            float3 direction = float3.zero;
            if (found)
            {
                float3 toTarget = targetPos - playerPos;
                toTarget.y = 0f;
                if (math.lengthsq(toTarget) > 1e-6f)
                    direction = math.normalize(toTarget);
            }
            SystemAPI.SetComponent(playerEntity, new AimDirection { Value = direction });

            if (weapon.FireTimer > 0f)
                return;

            if (!found || math.lengthsq(direction) <= 1e-6f)
            {
                // Hold at "ready" — without this the timer keeps going negative
                // while idle and the first enemy triggers a burst to catch up.
                weapon.FireTimer = 0f;
                return;
            }

            weapon.FireTimer += weapon.FireInterval;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            Entity prefab = weapon.ProjectilePrefab;
            Entity projectile = ecb.Instantiate(prefab);

            LocalTransform transformValue = SystemAPI.GetComponent<LocalTransform>(prefab);
            float3 spawnPos = playerPos;
            spawnPos.y += weapon.MuzzleHeight;
            transformValue.Position = spawnPos;

            // Aim the projectile's +Z axis along the flight direction so
            // arrow-style visuals fly nose-first. Spherical bullets don't
            // care about rotation, so this is harmless for the old prefab.
            transformValue.Rotation = quaternion.LookRotationSafe(direction, math.up());

            ecb.SetComponent(projectile, transformValue);
            ecb.SetComponent(projectile, new MoveDirection { Value = direction });
            ecb.SetComponent(projectile, new MoveSpeed { Value = weapon.ProjectileSpeed });
            ecb.SetComponent(projectile, new Damage { Value = weapon.ProjectileDamage });
            ecb.SetComponent(projectile, new Lifetime { Value = weapon.ProjectileLifetime });

            // Emit a one-shot event so the visual companion (PlayerVisualSync)
            // can fire the bow Shoot animation in lockstep with the projectile.
            Entity shotEvent = ecb.CreateEntity();
            ecb.AddComponent<PlayerShotEvent>(shotEvent);
        }
    }
}
