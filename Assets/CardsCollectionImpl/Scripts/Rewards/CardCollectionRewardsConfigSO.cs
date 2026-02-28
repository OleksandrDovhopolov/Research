using UnityEngine;

namespace core
{
    [CreateAssetMenu(
        fileName = "CardCollectionRewardsConfig",
        menuName = "Card Collection/Rewards Config",
        order = 0)]
    public class CardCollectionRewardsConfigSO : ScriptableObject
    {
        public GroupRewardDefinition[] GroupRewards;
        public CollectionRewardDefinition CollectionReward;
    }
}
