using System;
using System.Collections.Generic;
using CardCollection.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class AddressablesCardsConfigProvider : BaseAddressablesProvider<List<CardConfig>, TextAsset>, ICardsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<CardConfig> cards;
        }
        
        protected override List<CardConfig> ParseAsset(TextAsset asset)
        {
            return JsonConvert.DeserializeObject<Wrapper>(asset.text).cards;
        }
    }
}