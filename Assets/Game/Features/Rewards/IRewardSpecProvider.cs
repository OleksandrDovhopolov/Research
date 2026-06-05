using UnityEngine;

namespace Rewards
{
    public interface IRewardSpecProvider
    {
        bool TryGet(string rewardId, out RewardSpec spec);

        /// <summary>
        /// Looks up an icon for an inventory/resource id. Used by UI surfaces (Inventory, etc.)
        /// to keep icons in sync with the reward spec catalog instead of resolving Addressables
        /// independently. Falls back to top-level RewardSpec.Icon if no resource match is found.
        /// </summary>
        bool TryGetResourceIcon(string resourceId, out Sprite icon);
    }
}