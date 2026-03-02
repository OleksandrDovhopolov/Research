using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public abstract class CollectionRewardDefinition
    {
        public RewardSource Source = RewardSource.Unknown;
        public List<CardPack> CardPack = new();
    }
}