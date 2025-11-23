using System;
using Newtonsoft.Json;

namespace core
{
    [Serializable]
    public class CardCollectionConfig
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("cardName")] public string CardName;
        [JsonProperty("groupType")] public CardCollectionGroups GroupType;
        [JsonProperty("stars")] public int Start;
        [JsonProperty("premiumCard")] public bool PremiumCard;
    }
}