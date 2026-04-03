using System;
using System.Collections.Generic;

namespace CardCollection.Core
{
    [Serializable]
    public class EventConfig
    {
        public List<CardConfig> cards;
        public List<CardCollectionGroupConfig> groups;
        public List<RewardConfig> rewards;
        //public List<PackData> packs;
        //public List<OfferData> offers;
    }
}