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
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var enemies = _enemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyTransforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            _healthLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Single-threaded: multiple projectiles may damage the same enemy in
            // one frame, so the Health read-modify-write must not run in parallel.
            state.Dependency = new DamageJob
            {
                Enemies = enemies,
                EnemyTransforms = enemyTransforms,
                HealthLookup = _healthLookup,
                Ecb = ecb
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

                // Killing blow only — exactly one projectile marks the enemy dead.
                if (before > 0f && health.Value <= 0f)
                    Ecb.AddComponent<DeadTag>(enemy);

                Ecb.AddComponent<DeadTag>(projectile); // projectile is consumed
                break;
            }
        }
    }
}
