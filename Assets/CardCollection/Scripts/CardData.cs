using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace core
{    
    [Serializable]
    public class CardCollectionData 
    {
        public string CollectionName;
        public List<CardData> Cards;
    }

    [Serializable]
    public class CardData
    {
        [JsonProperty("id")] 
        public int CardId;

        [JsonProperty("cardName")] 
        public string CardName;

        [JsonProperty("groupType")] 
        public string GroupType;

        [JsonProperty("stars")] 
        public int Stars;

        [JsonProperty("premiumCard")] 
        public bool PremiumCard;
    }
}

