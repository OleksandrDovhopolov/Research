using Unity.Entities;

namespace Survival
{
    // One-shot tag added by LevelUpSystem when the player crosses the XP
    // threshold. Removed by LevelUpBridge when it opens the modal.
    public struct LevelUpRequest : IComponentData
    {
    }
}
