using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Survival
{
    // Copies the player's position into the PlayerPosition singleton on the main
    // thread. Because no job writes PlayerPosition, enemy systems and the camera
    // can read it without conflicting with the scheduled movement jobs.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    [BurstCompile]
    public partial struct PlayerPositionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, playerPosition) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerPosition>>().WithAll<PlayerTag>())
            {
                playerPosition.ValueRW.Value = transform.ValueRO.Position;
            }
        }
    }
}
