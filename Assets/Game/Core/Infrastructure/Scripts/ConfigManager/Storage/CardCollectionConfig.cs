using System;
using Newtonsoft.Json;

namespace Infrastructure
{
    [Serializable]
    public class CardCollectionConfig
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("cardName")] public string CardName;
        [JsonProperty("groupType")] public string GroupType;
        [JsonProperty("stars")] public int Stars;
        [JsonProperty("premiumCard")] public bool PremiumCard;
        [JsonProperty("icon")] public string Icon;
    }
}