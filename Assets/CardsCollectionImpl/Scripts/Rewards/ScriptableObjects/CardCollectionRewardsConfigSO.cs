using CardCollection.Core;
using UnityEngine;

namespace CardCollectionImpl
{
    [CreateAssetMenu(
        fileName = "CardCollectionRewardsConfig",
        menuName = "Card Collection/Rewards Config",
        order = 0)]
    public class CardCollectionRewardsConfigSO : ScriptableObject
    {
        public CollectionCompletionRewardConfig[] GroupRewards;
        public FullCollectionRewardConfig FullCollectionReward;
    }
}
