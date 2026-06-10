using Unity.Burst;
using Unity.Collections;
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
        // Tunables for inter-enemy separation (so zombies don't stack on one
        // point). Hardcoded — no authoring/prefab edits required.
        private const float SeparationRadius = 2.5f;     // body-width spacing
        private const float SeparationStrength = 3.0f;   // m/s push at contact

        private EntityQuery _enemyQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerPosition>();
            _enemyQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Snapshot all enemy positions for the separation pass. O(n²) inside
            // the job — fine for a few hundred enemies under Burst.
            NativeArray<LocalTransform> allEnemies =
                _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

            state.Dependency = new EnemyMoveJob
            {
                PlayerPos = SystemAPI.GetSingleton<PlayerPosition>().Value,
                DeltaTime = SystemAPI.Time.DeltaTime,
                AllEnemies = allEnemies,
                SeparationRadius = SeparationRadius,
                SeparationStrength = SeparationStrength
            }.ScheduleParallel(state.Dependency);

            allEnemies.Dispose(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(EnemyTag))]
    public partial struct EnemyMoveJob : IJobEntity
    {
        public float3 PlayerPos;
        public float DeltaTime;
        [ReadOnly] public NativeArray<LocalTransform> AllEnemies;
        public float SeparationRadius;
        public float SeparationStrength;

        private void Execute(ref LocalTransform transform,
            in MoveSpeed speed,
            in RotationSpeed rotationSpeed,
            in ContactDamage cd)
        {
            float3 toPlayer = PlayerPos - transform.Position;
            toPlayer.y = 0f;
            float distSq = math.lengthsq(toPlayer);

            // Separation: push away from any neighbor within SeparationRadius,
            // with linear falloff (strongest when touching). Self is skipped
            // via the tiny-distance guard.
            float3 separation = float3.zero;
            float sepRadSq = SeparationRadius * SeparationRadius;
            for (int i = 0; i < AllEnemies.Length; i++)
            {
                float3 delta = transform.Position - AllEnemies[i].Position;
                delta.y = 0f;
                float dSq = math.lengthsq(delta);
                if (dSq < 1e-4f || dSq > sepRadSq)
                    continue;
                float d = math.sqrt(dSq);
                float weight = 1f - d / SeparationRadius;
                separation += (delta / d) * weight;
            }
            separation *= SeparationStrength;

            // Rotate toward the player every frame (even while standing still
            // at melee range — the zombie must face the target before striking).
            if (distSq > 1e-6f)
            {
                float3 direction = math.normalize(toPlayer);
                quaternion target = quaternion.LookRotationSafe(direction, math.up());
                transform.Rotation = math.slerp(transform.Rotation, target,
                    math.saturate(rotationSpeed.Value * DeltaTime));

                // Approach only while outside the contact (melee) radius;
                // separation, however, always applies — that's what keeps the
                // melee ring from collapsing into a single point.
                float3 velocity = separation;
                float stopDist = cd.Radius;
                if (distSq > stopDist * stopDist)
                    velocity += direction * speed.Value;

                transform.Position += velocity * DeltaTime;
            }
            else
            {
                // Sitting on the player — apply separation only.
                transform.Position += separation * DeltaTime;
            }
        }
    }
}
