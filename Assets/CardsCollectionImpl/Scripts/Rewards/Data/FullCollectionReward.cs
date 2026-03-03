using System.Collections.Generic;
using CardCollection.Core;
using Resources.Core;

namespace CardCollectionImpl
{
    public class FullCollectionReward : CollectionRewardDefinition
    {
        public List<GameResource> Resources = new();
    }
}