using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMoveToPlayerSystem))]
    [BurstCompile]
    public partial struct EnemyContactDamageSystem : ISystem
    {
        private ComponentLookup<Health> _healthLookup;

        public void OnCreate(ref SystemState state)
        {
            _healthLookup = state.GetComponentLookup<Health>(false);
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<PlayerPosition>();
            state.RequireForUpdate<ContactDamage>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            float3 playerPos = SystemAPI.GetSingleton<PlayerPosition>().Value;
            _healthLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Single-threaded: many enemies may damage the same player.Health in
            // one frame — RMW must not run in parallel.
            state.Dependency = new EnemyContactDamageJob
            {
                PlayerEntity = playerEntity,
                PlayerPos = playerPos,
                DeltaTime = SystemAPI.Time.DeltaTime,
                HealthLookup = _healthLookup,
                Ecb = ecb
            }.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(EnemyTag))]
    [WithNone(typeof(DeadTag))]
    public partial struct EnemyContactDamageJob : IJobEntity
    {
        public Entity PlayerEntity;
        public float3 PlayerPos;
        public float DeltaTime;
        public ComponentLookup<Health> HealthLookup;
        public EntityCommandBuffer Ecb;

        private void Execute(in LocalTransform transform, ref ContactDamage cd)
        {
            cd.Timer = math.max(0f, cd.Timer - DeltaTime);

            float2 d = transform.Position.xz - PlayerPos.xz;
            if (math.lengthsq(d) > cd.Radius * cd.Radius)
                return;
            if (cd.Timer > 0f)
                return;

            Health hp = HealthLookup[PlayerEntity];
            hp.Value -= cd.DamagePerHit;
            HealthLookup[PlayerEntity] = hp;

            cd.Timer = cd.Interval;

            // Emit a DamageEvent so the HUD can show a floating "-X".
            Entity dmgEvent = Ecb.CreateEntity();
            Ecb.AddComponent(dmgEvent, new DamageEvent
            {
                Position = PlayerPos,
                Amount = cd.DamagePerHit,
                ToPlayer = true
            });
        }
    }
}
