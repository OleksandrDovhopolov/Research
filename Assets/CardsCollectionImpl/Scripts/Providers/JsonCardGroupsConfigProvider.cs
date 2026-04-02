using System;
using System.Collections.Generic;
using CardCollection.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class JsonCardGroupsConfigProvider : BaseAddressablesProvider<List<CardCollectionGroupConfig>, TextAsset>, ICardGroupsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<CardCollectionGroupConfig> groups;
        }
        
        protected override List<CardCollectionGroupConfig> ParseAsset(TextAsset asset)
        {
            return JsonConvert.DeserializeObject<Wrapper>(asset.text).groups;
        }
    }
}
