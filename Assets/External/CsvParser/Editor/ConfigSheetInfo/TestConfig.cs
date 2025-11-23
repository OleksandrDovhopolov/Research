using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace core
{
    [Serializable]
    public class TestConfig
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("count")] public int Count;
        [JsonProperty("recipes")] public List<int> Recipes;
    }
}