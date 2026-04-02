using CardCollection.Core;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;

namespace CardCollectionImpl
{
    [CreateAssetMenu(
        fileName = "CardsEventRewardsConfig",
        menuName = "Card Collection/Rewards/EventRewardsConfig")]
    public class CardEventRewardsConfigSO : ScriptableObject
    {
        [System.Serializable]
        private sealed class RewardsRoot
        {
            public RewardConfig[] rewards;
        }

        public TextAsset GroupsJson;
        public CollectionCompletionRewardConfig[] EventRewardsList;

        public override string ToString()
        {
            var rewards = EventRewardsList?
                .Select(x => new RewardConfig
                {
                    rewardId = x.GroupId,
                    rewardItemId = x.RewardId
                })
                .ToArray() ?? new RewardConfig[0];

            var root = new RewardsRoot
            {
                rewards = rewards
            };

            return JsonConvert.SerializeObject(root, Formatting.Indented);
        }
    }
}