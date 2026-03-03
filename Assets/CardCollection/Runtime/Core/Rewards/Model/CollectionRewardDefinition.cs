using System.Collections.Generic;

namespace CardCollection.Core
{
    public abstract class CollectionRewardDefinition
    {
        public RewardSource Source = RewardSource.Unknown;
        public List<CardPack> CardPack = new();
    }
}