using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace core
{
    public enum CardCollectionGroups
    {
        Farming,
        Worksite,
        Railroads,
        Factories,
        NextDoor,
        Delivery,
        Mining,
        Picnic,
        FarmStore,
        Animals,
        Museum,
        Cafe,
        BigCity,
        Park,
        TownHall
    }
    
    [Serializable]
    public class CardCollectionData 
    {
        public string CollectionName;
        public List<CardData> Cards;
    }

    /*[Serializable]
    public class CollectionGroup
    {
        public int GroupId;
        public CardCollectionGroups Gropu;
        public List<CardData> GroupCollection;
    }*/

    [Serializable]
    public class CardData
    {
        [JsonProperty("id")] 
        public int CardId;

        [JsonProperty("cardName")] 
        public string CardName;

        [JsonProperty("groupType")] 
        public CardCollectionGroups GroupType;

        [JsonProperty("stars")] 
        public int Stars;

        [JsonProperty("premiumCard")] 
        public bool PremiumCard;
    }
}

