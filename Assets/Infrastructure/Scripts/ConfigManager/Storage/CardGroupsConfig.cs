using System;
using Newtonsoft.Json;

namespace Infrastructure
{
    [Serializable]
    public class CardGroupsConfig : ConfigBase
    {
        [JsonProperty("ID")] public string Id;
        [JsonProperty("groupType")] public string GroupType;
        [JsonProperty("groupName")] public string GroupName;
        [JsonProperty("groupIcon")] public string GroupIcon;
    }
}
