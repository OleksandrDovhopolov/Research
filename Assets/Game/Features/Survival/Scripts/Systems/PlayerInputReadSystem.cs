using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PlayerMoveSystem))]
    public partial struct PlayerInputReadSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            Debug.Log("[Survival] PlayerInputReadSystem.OnCreate — system created");
        }

        // No [BurstCompile]: reads UnityEngine.Input and the managed input bridge.
        public void OnUpdate(ref SystemState state)
        {
            float2 keyboard = new float2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            float2 joystick = PlayerInputBridge.JoystickActive
                ? new float2(PlayerInputBridge.JoystickAxis.x, PlayerInputBridge.JoystickAxis.y)
                : float2.zero;

            float2 combined = math.lengthsq(joystick) > 1e-4f ? joystick : keyboard;
            if (math.lengthsq(combined) > 1f)
                combined = math.normalize(combined);

            float3 direction = new float3(combined.x, 0f, combined.y);

            int playerCount = 0;
            foreach (var moveDirection in
                SystemAPI.Query<RefRW<MoveDirection>>().WithAll<PlayerTag>())
            {
                moveDirection.ValueRW.Value = direction;
                playerCount++;
            }

            if (math.lengthsq(combined) > 1e-4f)
            {
                Debug.Log($"[Survival] PlayerInputReadSystem.OnUpdate — keyboard={keyboard} joystick={joystick} dir={direction} players matched={playerCount}");
            }
        }
    }
}
