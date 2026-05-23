using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct EnemyRunBobSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunBob>();
            _transformLookup = state.GetComponentLookup<LocalTransform>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);

            new EnemyRunBobJob
            {
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                TransformLookup = _transformLookup
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct EnemyRunBobJob : IJobEntity
    {
        public float ElapsedTime;

        // Each root writes to its own unique child via LinkedEntityGroup[1] —
        // no overlap across enemies, so parallel writes are safe.
        [NativeDisableParallelForRestriction]
        public ComponentLookup<LocalTransform> TransformLookup;

        private void Execute(Entity rootEntity, in RunBob bob,
            in DynamicBuffer<LinkedEntityGroup> linkedGroup)
        {
            if (linkedGroup.Length < 2)
                return; // no mesh child yet

            Entity meshChild = linkedGroup[1].Value;
            if (!TransformLookup.HasComponent(meshChild))
                return;

            // Per-entity stable offset desyncs the bob across enemies.
            var rng = Random.CreateFromIndex((uint)rootEntity.Index);
            float offset = rng.NextFloat(0f, math.PI * 2f);
            float phase = ElapsedTime * bob.Speed + offset;

            // Side-to-side sway + constant forward lean. No vertical motion —
            // keeps the mesh on the floor (no dipping below).
            float sway = math.sin(phase) * bob.SwayRadians;

            LocalTransform transform = TransformLookup[meshChild];
            transform.Position = float3.zero;
            transform.Rotation = math.mul(
                quaternion.RotateX(bob.LeanRadians),
                quaternion.RotateZ(sway));
            TransformLookup[meshChild] = transform;
        }
    }
}
