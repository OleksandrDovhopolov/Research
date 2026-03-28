using System.Collections.Generic;
using CardCollection.Core;
using Rewards;

namespace CardCollectionImpl
{
    public abstract class CollectionRewardDefinition
    {
        public RewardSource Source = RewardSource.Unknown;
        
        public abstract IEnumerable<RewardGrantRequest> ToRequests();
    }
}