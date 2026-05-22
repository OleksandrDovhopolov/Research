using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerPositionSystem))]
    [BurstCompile]
    public partial struct EnemyMoveToPlayerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerPosition>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new EnemyMoveJob
            {
                PlayerPos = SystemAPI.GetSingleton<PlayerPosition>().Value,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(EnemyTag))]
    public partial struct EnemyMoveJob : IJobEntity
    {
        public float3 PlayerPos;
        public float DeltaTime;

        private void Execute(ref LocalTransform transform,
            in MoveSpeed speed, in RotationSpeed rotationSpeed)
        {
            float3 toPlayer = PlayerPos - transform.Position;
            toPlayer.y = 0f;
            if (math.lengthsq(toPlayer) <= 1e-6f)
                return;

            float3 direction = math.normalize(toPlayer);
            transform.Position += direction * speed.Value * DeltaTime;

            quaternion target = quaternion.LookRotationSafe(direction, math.up());
            transform.Rotation = math.slerp(transform.Rotation, target,
                math.saturate(rotationSpeed.Value * DeltaTime));
        }
    }
}
