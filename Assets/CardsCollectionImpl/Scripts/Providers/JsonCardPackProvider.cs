using System;
using System.Collections.Generic;
using CardCollection.Core;
using UnityEngine;

namespace CardCollectionImpl
{
    public class JsonCardPackProvider : BaseJsonProvider<List<CardPackConfig>>, ICardPackProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<CardPackConfig> packs;
        }

        protected override List<CardPackConfig> CreateDefault() => new();

        protected override List<CardPackConfig> ParseJson(string json)
        {
            return JsonUtility.FromJson<Wrapper>(json).packs;
        }
    }
}
