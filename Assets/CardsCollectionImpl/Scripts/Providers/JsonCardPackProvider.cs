using System;
using System.Collections.Generic;
using CardCollection.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class JsonCardPackProvider : BaseAddressablesProvider<List<CardPackConfig>, TextAsset>, ICardPackProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<CardPackConfig> packs;
        }

        protected override List<CardPackConfig> ParseAsset(TextAsset textAsset)
        {
            return JsonConvert.DeserializeObject<Wrapper>(textAsset.text).packs;
        }
    }
}
