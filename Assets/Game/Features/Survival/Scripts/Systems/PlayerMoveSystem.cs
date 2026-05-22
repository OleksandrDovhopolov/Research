using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
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
            float2 min = new float2(float.MinValue);
            float2 max = new float2(float.MaxValue);
            if (SystemAPI.TryGetSingleton<ArenaBounds>(out var bounds))
            {
                min = bounds.Min;
                max = bounds.Max;
            }

            new PlayerMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Min = min,
                Max = max
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    [WithAll(typeof(PlayerTag))]
    public partial struct PlayerMoveJob : IJobEntity
    {
        public float DeltaTime;
        public float2 Min; // .x = world X, .y = world Z
        public float2 Max;

        private void Execute(ref LocalTransform transform,
            in MoveDirection direction, in MoveSpeed speed, in RotationSpeed rotationSpeed)
        {
            float3 position = transform.Position + direction.Value * speed.Value * DeltaTime;
            position.x = math.clamp(position.x, Min.x, Max.x);
            position.z = math.clamp(position.z, Min.y, Max.y);
            transform.Position = position;

            if (math.lengthsq(direction.Value) > 1e-6f)
            {
                quaternion target = quaternion.LookRotationSafe(direction.Value, math.up());
                transform.Rotation = math.slerp(transform.Rotation, target,
                    math.saturate(rotationSpeed.Value * DeltaTime));
            }
        }
    }
}
