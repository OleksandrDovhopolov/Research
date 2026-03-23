using System.Collections.Generic;
using System.Linq;
using Resources.Core;
using Rewards;

namespace CardCollectionImpl
{
    public class FullCollectionReward : CollectionRewardDefinition
    {
        public List<GameResource> Resources = new();
        
        public override IEnumerable<RewardGrantRequest> ToRequests()
        {
            return Resources
                .Where(r => r.Amount > 0)
                .Select(r => new RewardGrantRequest(r.Type.ToString(), r.Amount));
        }
    }
}