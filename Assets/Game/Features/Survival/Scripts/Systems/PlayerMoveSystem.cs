using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct PlayerMoveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new PlayerMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(PlayerTag))]
    public partial struct PlayerMoveJob : IJobEntity
    {
        public float DeltaTime;

        private void Execute(ref LocalTransform transform,
            in MoveDirection direction, in MoveSpeed speed)
        {
            transform.Position += direction.Value * speed.Value * DeltaTime;
        }
    }
}
