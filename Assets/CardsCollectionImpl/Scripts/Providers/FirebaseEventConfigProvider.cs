using System;
using CardCollection.Core;
using Unity.Plastic.Newtonsoft.Json;
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
            var test = textAsset.text;
            Debug.LogWarning($"Debug test {test}");
            var result = JsonConvert.DeserializeObject<Wrapper>(textAsset.text).eventConfigs;
            Debug.LogWarning($"Debug result {result}");
            return result;
        }
    }
}