using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct PlayerMoveSystem : ISystem
    {
        private bool _logged; // TEMP DEBUG

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            Debug.Log("[Survival] PlayerMoveSystem.OnCreate — system created");
        }

        // TEMP DEBUG: [BurstCompile] removed from OnUpdate so Debug.Log is allowed.
        // Re-add [BurstCompile] when debugging is done — the job below stays bursted.
        public void OnUpdate(ref SystemState state)
        {
            float2 min = new float2(float.MinValue);
            float2 max = new float2(float.MaxValue);
            bool hasBounds = SystemAPI.TryGetSingleton<ArenaBounds>(out var bounds);
            if (hasBounds)
            {
                min = bounds.Min;
                max = bounds.Max;
            }

            if (!_logged)
            {
                Debug.Log($"[Survival] PlayerMoveSystem.OnUpdate — running. ArenaBounds found={hasBounds} min={min} max={max}");
                _logged = true;
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
            in MoveDirection direction, in MoveSpeed speed)
        {
            float3 position = transform.Position + direction.Value * speed.Value * DeltaTime;
            position.x = math.clamp(position.x, Min.x, Max.x);
            position.z = math.clamp(position.z, Min.y, Max.y);
            transform.Position = position;
        }
    }
}
