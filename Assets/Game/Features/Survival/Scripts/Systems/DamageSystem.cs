using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMoveSystem))]
    [UpdateAfter(typeof(EnemyMoveToPlayerSystem))]
    [BurstCompile]
    public partial struct DamageSystem : ISystem
    {
        private EntityQuery _enemyQuery;
        private ComponentLookup<Health> _healthLookup;

        public void OnCreate(ref SystemState state)
        {
            _enemyQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnemyTag, LocalTransform, Health>()
                .WithNone<DeadTag>()
                .Build(ref state);

            _healthLookup = state.GetComponentLookup<Health>(false);

            state.RequireForUpdate<ProjectileTag>();
            state.RequireForUpdate<EnemyTag>();
            state.RequireForUpdate<XpDropConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var enemies = _enemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyTransforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            _healthLookup.Update(ref state);

            var xpConfig = SystemAPI.GetSingleton<XpDropConfig>();
            LocalTransform xpBase = xpConfig.XpPrefab != Entity.Null
                ? SystemAPI.GetComponent<LocalTransform>(xpConfig.XpPrefab)
                : default;

            // BeginSimulation ECB so Instantiate + SetComponent for the XP gem
            // play back before TransformSystemGroup — no one-frame flash at origin.
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Single-threaded: multiple projectiles may damage the same enemy in
            // one frame, so the Health read-modify-write must not run in parallel.
            state.Dependency = new DamageJob
            {
                Enemies = enemies,
                EnemyTransforms = enemyTransforms,
                HealthLookup = _healthLookup,
                Ecb = ecb,
                XpPrefab = xpConfig.XpPrefab,
                XpPerKill = xpConfig.XpPerKill,
                XpBaseTransform = xpBase,
                XpSpawnHeightOffset = xpConfig.SpawnHeightOffset
            }.Schedule(state.Dependency);

            enemies.Dispose(state.Dependency);
            enemyTransforms.Dispose(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(ProjectileTag))]
    [WithNone(typeof(DeadTag))]
    public partial struct DamageJob : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> Enemies;
        [ReadOnly] public NativeArray<LocalTransform> EnemyTransforms;
        public ComponentLookup<Health> HealthLookup;
        public EntityCommandBuffer Ecb;

        public Entity XpPrefab;
        public int XpPerKill;
        public LocalTransform XpBaseTransform;
        public float XpSpawnHeightOffset;

        private void Execute(Entity projectile, in LocalTransform transform,
            in Damage damage, in HitRadius hitRadius)
        {
            float radiusSq = hitRadius.Value * hitRadius.Value;

            for (int i = 0; i < Enemies.Length; i++)
            {
                Entity enemy = Enemies[i];
                Health health = HealthLookup[enemy];
                if (health.Value <= 0f)
                    continue; // already a corpse this frame

                float distSq = math.distancesq(transform.Position.xz, EnemyTransforms[i].Position.xz);
                if (distSq > radiusSq)
                    continue;

                float before = health.Value;
                health.Value -= damage.Value;
                HealthLookup[enemy] = health;

                // Emit a DamageEvent so the HUD can show a floating "-X".
                Entity dmgEvent = Ecb.CreateEntity();
                Ecb.AddComponent(dmgEvent, new DamageEvent
                {
                    Position = EnemyTransforms[i].Position,
                    Amount = damage.Value,
                    ToPlayer = false
                });

                // Killing blow only — exactly one projectile marks the enemy dead
                // and exactly one XP gem is dropped at the enemy's position.
                if (before > 0f && health.Value <= 0f)
                {
                    Ecb.AddComponent<DeadTag>(enemy);

                    if (XpPrefab != Entity.Null)
                    {
                        LocalTransform gemTransform = XpBaseTransform;
                        float3 enemyPos = EnemyTransforms[i].Position;
                        gemTransform.Position = new float3(
                            enemyPos.x,
                            enemyPos.y + XpSpawnHeightOffset,
                            enemyPos.z);
                        Entity gem = Ecb.Instantiate(XpPrefab);
                        Ecb.SetComponent(gem, gemTransform);
                        Ecb.SetComponent(gem, new XpValue { Value = XpPerKill });
                    }
                }

                Ecb.AddComponent<DeadTag>(projectile); // projectile is consumed
                break;
            }
        }
    }
}
