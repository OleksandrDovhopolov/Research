using System;
using System.Collections.Generic;
using CardCollection.Core;
using Newtonsoft.Json;

namespace CardCollectionImpl
{
    public sealed class JsonCardGroupsConfigProvider : BaseJsonProvider<List<CardCollectionGroupConfig>>, ICardGroupsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<CardCollectionGroupConfig> groups;
        }

        protected override List<CardCollectionGroupConfig> CreateDefault() => new();
        
        protected override List<CardCollectionGroupConfig> ParseJson(string json)
        {
            return JsonConvert.DeserializeObject<Wrapper>(json).groups;
        }
    }
}
