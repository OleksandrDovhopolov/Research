using System;
using System.Collections.Generic;
using CardCollection.Core;
using Newtonsoft.Json;

namespace CardCollectionImpl
{
    public class JsonCardsConfigProvider : BaseJsonProvider<List<CardConfig>>, ICardsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<CardConfig> cards;
        }
        
        protected override List<CardConfig> ParseJson(string json)
        {
            return JsonConvert.DeserializeObject<Wrapper>(json).cards;
        }

        protected override List<CardConfig> CreateDefault() => new();
    }
}