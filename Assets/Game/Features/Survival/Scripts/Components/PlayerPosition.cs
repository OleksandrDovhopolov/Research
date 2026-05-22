using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    // Single instance (on the player) — used as a singleton.
    // Written each frame by PlayerMoveJob; read by enemy systems and the camera.
    public struct PlayerPosition : IComponentData
    {
        public float3 Value;
    }
}
