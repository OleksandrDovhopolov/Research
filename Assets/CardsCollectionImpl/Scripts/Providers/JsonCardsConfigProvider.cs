using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public class JsonCardsConfigProvider : BaseJsonProvider<List<CardConfig>>, ICardsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<CardConfig> cards;
        }
        
        protected override string ConfigFileName => "cardCollection";
        
        protected override List<CardConfig> ParseJson(string json)
        {
            return JsonUtility.FromJson<Wrapper>(json).cards;
        }

        protected override List<CardConfig> CreateDefault() => new();

        public UniTask<List<CardConfig>> GetCardGroupsConfigsAsync(string eventId, CancellationToken ct = default)
        {
            return GetDataAsync(ct);
        }

        public void ClearCache()
        {
            Dispose();
        }
    }
}