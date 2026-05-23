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

            _experienceLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Single-threaded to keep the player's Experience read-modify-write safe.
            state.Dependency = new PickupXpJob
            {
                PlayerEntity = playerEntity,
                PlayerPos = playerPos,
                PickupRadiusSq = pickupRadius * pickupRadius,
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
        public ComponentLookup<Experience> ExperienceLookup;
        public EntityCommandBuffer Ecb;

        private void Execute(Entity gem, in LocalTransform transform, in XpValue xp)
        {
            float2 delta = transform.Position.xz - PlayerPos.xz;
            if (math.lengthsq(delta) > PickupRadiusSq)
                return;

            Experience exp = ExperienceLookup[PlayerEntity];
            exp.Current += xp.Value;
            ExperienceLookup[PlayerEntity] = exp;

            Ecb.AddComponent<DeadTag>(gem);
        }
    }
}
