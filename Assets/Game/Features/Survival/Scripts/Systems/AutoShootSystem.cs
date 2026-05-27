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
            state.RequireForUpdate<WeaponBurstState>();
            state.RequireForUpdate<PlayerPosition>();
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var weapon = ref SystemAPI.GetSingletonRW<Weapon>().ValueRW;
            ref var burst = ref SystemAPI.GetSingletonRW<WeaponBurstState>().ValueRW;

            float dt = SystemAPI.Time.DeltaTime;
            weapon.FireTimer -= dt;
            burst.NextShotTimer -= dt;

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

            if (math.lengthsq(direction) <= 1e-6f)
            {
                // Нет цели — холдим оба таймера на нуле чтобы не накапливалось
                // негативное значение и не выстреливал залпом сразу при появлении.
                weapon.FireTimer = math.max(0f, weapon.FireTimer);
                burst.NextShotTimer = math.max(0f, burst.NextShotTimer);
                return;
            }

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            Entity prefab = weapon.ProjectilePrefab;
            LocalTransform baseTransform = SystemAPI.GetComponent<LocalTransform>(prefab);

            // 1) Burst follow-up shot — приоритет выше чем main, потому что
            //    он уже "оплачен" предыдущим FireTimer'ом.
            if (burst.RemainingShots > 0 && burst.NextShotTimer <= 0f)
            {
                FireVolley(ecb, in weapon, prefab, baseTransform, playerPos, direction);
                burst.RemainingShots--;
                burst.NextShotTimer = weapon.BurstDelay;
            }

            // 2) Main shot — стартует следующий burst chain.
            if (weapon.FireTimer <= 0f)
            {
                FireVolley(ecb, in weapon, prefab, baseTransform, playerPos, direction);
                weapon.FireTimer += weapon.FireInterval;
                burst.RemainingShots = math.max(0, weapon.BurstCount - 1);
                burst.NextShotTimer = weapon.BurstDelay;
            }
        }

        // Spawns one volley (MultiShot fan) + emits a single PlayerShotEvent.
        // Pure static helper so it stays Burst-compatible — all dependencies
        // passed as parameters; no SystemAPI lookups inside.
        private static void FireVolley(EntityCommandBuffer ecb, in Weapon weapon,
            Entity prefab, in LocalTransform baseTransform,
            float3 playerPos, float3 direction)
        {
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

            // ONE PlayerShotEvent per volley — анимация натяжения лука должна
            // сработать на каждой стреле залпа (и для burst follow-up тоже).
            Entity shotEvent = ecb.CreateEntity();
            ecb.AddComponent<PlayerShotEvent>(shotEvent);
        }
    }
}
