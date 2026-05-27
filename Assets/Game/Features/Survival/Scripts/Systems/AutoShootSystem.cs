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
            LocalTransform baseTransform = SystemAPI.GetComponent<LocalTransform>(prefab);
            float3 spawnPos = playerPos;
            spawnPos.y += weapon.MuzzleHeight;

            int shotCount = math.max(1, weapon.ProjectileCount);
            // Total fan angle растёт от 0 (один снаряд) до SpreadAngle*(N-1).
            // Снаряды распределяются симметрично вокруг направления на цель.
            float totalSpreadRad = math.radians(weapon.SpreadAngle) * (shotCount - 1);

            for (int i = 0; i < shotCount; i++)
            {
                float t = shotCount > 1
                    ? (i / (float)(shotCount - 1)) - 0.5f
                    : 0f;
                float angle = t * totalSpreadRad;
                quaternion yawOffset = quaternion.RotateY(angle);
                float3 shotDir = math.mul(yawOffset, direction);

                LocalTransform transformValue = baseTransform;
                transformValue.Position = spawnPos;
                // Aim the projectile's +Z axis along its own flight direction so
                // arrow-style visuals fly nose-first.
                transformValue.Rotation = quaternion.LookRotationSafe(shotDir, math.up());

                Entity projectile = ecb.Instantiate(prefab);
                ecb.SetComponent(projectile, transformValue);
                ecb.SetComponent(projectile, new MoveDirection { Value = shotDir });
                ecb.SetComponent(projectile, new MoveSpeed { Value = weapon.ProjectileSpeed });
                ecb.SetComponent(projectile, new Damage { Value = weapon.ProjectileDamage });
                ecb.SetComponent(projectile, new Lifetime { Value = weapon.ProjectileLifetime });
            }

            // Emit ONE PlayerShotEvent per volley (not per projectile) so the
            // bow Shoot animation triggers once. N events would restart the
            // anim N frames in a row.
            Entity shotEvent = ecb.CreateEntity();
            ecb.AddComponent<PlayerShotEvent>(shotEvent);
        }
    }
}
