using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ProjectileMoveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectileTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ProjectileMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(ProjectileTag))]
    public partial struct ProjectileMoveJob : IJobEntity
    {
        public float DeltaTime;

        private void Execute(ref LocalTransform transform,
            in MoveDirection direction, in MoveSpeed speed)
        {
            transform.Position += direction.Value * speed.Value * DeltaTime;
        }
    }
}
