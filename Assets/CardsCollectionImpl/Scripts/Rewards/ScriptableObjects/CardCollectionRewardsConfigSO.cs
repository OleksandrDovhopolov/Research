using CardCollection.Core;
using UnityEngine;

namespace CardCollectionImpl
{
    //TODO can be loaded by new provider
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
