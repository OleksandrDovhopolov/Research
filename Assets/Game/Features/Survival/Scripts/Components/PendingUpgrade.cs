using Unity.Entities;

namespace Survival
{
    // Set by LevelUpBridge once the user picks an upgrade card; consumed by
    // ApplyUpgradeSystem the next frame.
    public struct PendingUpgrade : IComponentData
    {
        public UpgradeType Type;
        public float Value;
    }
}
