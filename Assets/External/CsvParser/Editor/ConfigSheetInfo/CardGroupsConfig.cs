using System;
using Newtonsoft.Json;

namespace core
{
    [Serializable]
    public class CardGroupsConfig 
    {
         [JsonProperty("ID")] public string Id;
         [JsonProperty("groupType")] public string GroupType;
         [JsonProperty("groupName")] public string GroupName;
         [JsonProperty("groupIcon")] public string GroupIcon;
    }
}