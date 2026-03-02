using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    //TODO reward. collectionModuleBaseReward / baseReward / could be an offer and group/collection reward model 
    public abstract class OfferContent
    {
        public RewardSource Source = RewardSource.Unknown;
        public List<CardPack> CardPack = new();
    }
}