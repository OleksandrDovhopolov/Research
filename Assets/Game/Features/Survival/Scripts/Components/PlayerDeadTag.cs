using Unity.Entities;

namespace Survival
{
    // Sticky tag added by PlayerDeathSystem when the player's Health hits 0.
    // GameOverBridge watches for this and opens the Game Over modal.
    public struct PlayerDeadTag : IComponentData
    {
    }
}
