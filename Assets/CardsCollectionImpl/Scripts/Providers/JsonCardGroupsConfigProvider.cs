using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class JsonCardGroupsConfigProvider : BaseJsonProvider<List<CardCollectionGroupConfig>>, ICardGroupsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<CardCollectionGroupConfig> groups;
        }
        
        protected override string ConfigFileName => "cardGroups";

        protected override List<CardCollectionGroupConfig> CreateDefault() => new();
        
        protected override List<CardCollectionGroupConfig> ParseJson(string json)
        {
            return JsonUtility.FromJson<Wrapper>(json).groups;
        }

        public UniTask<List<CardCollectionGroupConfig>> GetCardGroupsConfigsAsync(string eventId, CancellationToken ct)
        {
            return GetDataAsync(ct);
        }

        public void ClearCache()
        {
            Dispose();
        }
    }
}
