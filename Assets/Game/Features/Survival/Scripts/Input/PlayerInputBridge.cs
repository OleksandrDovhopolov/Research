using UnityEngine;

namespace Survival
{
    // Main-thread-only transport between the joystick MonoBehaviour and the ECS
    // input reader. Burst code never touches this.
    public static class PlayerInputBridge
    {
        public static Vector2 JoystickAxis;
        public static bool JoystickActive;

        public static void Reset()
        {
            JoystickAxis = Vector2.zero;
            JoystickActive = false;
        }
    }
}
