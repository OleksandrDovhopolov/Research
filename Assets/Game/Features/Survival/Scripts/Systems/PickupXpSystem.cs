using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerPositionSystem))]
    [BurstCompile]
    public partial struct PickupXpSystem : ISystem
    {
        private ComponentLookup<Experience> _experienceLookup;

        public void OnCreate(ref SystemState state)
        {
            _experienceLookup = state.GetComponentLookup<Experience>(false);
            state.RequireForUpdate<XpPickupTag>();
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<PlayerPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            float3 playerPos = SystemAPI.GetSingleton<PlayerPosition>().Value;
            float pickupRadius = SystemAPI.GetComponent<PickupRadius>(playerEntity).Value;

            // Fallback when SubScene hasn't been rebaked yet: collapse magnet
            // down to the pickup radius (no pull, just absorb on touch).
            XpMagnet magnet = SystemAPI.HasComponent<XpMagnet>(playerEntity)
                ? SystemAPI.GetComponent<XpMagnet>(playerEntity)
                : new XpMagnet { Radius = pickupRadius, Speed = 0f };

            _experienceLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Single-threaded to keep the player's Experience read-modify-write
            // safe; magnet position writes are per-gem (unique entities) so
            // they're fine on the same thread too.
            state.Dependency = new PickupXpJob
            {
                PlayerEntity = playerEntity,
                PlayerPos = playerPos,
                PickupRadiusSq = pickupRadius * pickupRadius,
                MagnetRadiusSq = magnet.Radius * magnet.Radius,
                MagnetSpeed = magnet.Speed,
                DeltaTime = SystemAPI.Time.DeltaTime,
                ExperienceLookup = _experienceLookup,
                Ecb = ecb
            }.Schedule(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(XpPickupTag))]
    [WithNone(typeof(DeadTag))]
    public partial struct PickupXpJob : IJobEntity
    {
        public Entity PlayerEntity;
        public float3 PlayerPos;
        public float PickupRadiusSq;
        public float MagnetRadiusSq;
        public float MagnetSpeed;
        public float DeltaTime;
        public ComponentLookup<Experience> ExperienceLookup;
        public EntityCommandBuffer Ecb;

        private void Execute(Entity gem, ref LocalTransform transform, in XpValue xp)
        {
            float2 delta = PlayerPos.xz - transform.Position.xz;
            float distSq = math.lengthsq(delta);

            // Outside magnet range — gem just sits there.
            if (distSq > MagnetRadiusSq)
                return;

            // Inside absorb range — grant XP and kill the gem.
            if (distSq <= PickupRadiusSq)
            {
                Experience exp = ExperienceLookup[PlayerEntity];
                exp.Current += xp.Value;
                ExperienceLookup[PlayerEntity] = exp;

                Ecb.AddComponent<DeadTag>(gem);
                return;
            }

            // In magnet zone — fly toward the player on the XZ plane,
            // preserving the gem's authored Y (so it stays on the ground).
            float dist = math.sqrt(distSq);
            float2 dir = delta / dist;
            float2 step = dir * MagnetSpeed * DeltaTime;
            transform.Position += new float3(step.x, 0f, step.y);
        }
    }
}
