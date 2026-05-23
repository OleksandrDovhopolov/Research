using Unity.Entities;

namespace Survival
{
    // Side-to-side sway + constant forward lean for the enemy mesh child.
    // Applied each frame by EnemyRunBobSystem to fake a walking gait.
    public struct RunBob : IComponentData
    {
        public float SwayRadians;   // peak Z-axis sway
        public float Speed;         // phase advance, rad/sec
        public float LeanRadians;   // constant forward lean about X
    }
}
