using System.Collections.Generic;
using System.Linq;
using CoreResources;
using Rewards;

namespace CardCollectionImpl
{
    public class CardGroupCompletionReward : CollectionRewardDefinition
    {
        public List<GameResource> Resources { get; } = new();

        public override IEnumerable<RewardGrantRequest> ToRequests()
        {
            return Resources
                .Where(r => r.Amount > 0)
                .Select(r => new RewardGrantRequest(r.Type.ToString(), r.Amount));
        }
    }
}