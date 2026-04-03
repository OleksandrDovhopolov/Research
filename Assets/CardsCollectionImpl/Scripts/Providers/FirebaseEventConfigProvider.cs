using System;
using CardCollection.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public class FirebaseEventConfigProvider : BaseFirebaseProvider<EventConfig, TextAsset>, IEventConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public EventConfig eventConfigs;
        }
        
        protected override EventConfig ParseAsset(TextAsset textAsset)
        {
            return JsonConvert.DeserializeObject<Wrapper>(textAsset.text).eventConfigs;
        }
    }
}