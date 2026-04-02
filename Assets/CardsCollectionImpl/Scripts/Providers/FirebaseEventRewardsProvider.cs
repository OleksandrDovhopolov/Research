using System;
using System.Collections.Generic;
using CardCollection.Core;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class FirebaseEventRewardsProvider : BaseFirebaseProvider<List<RewardConfig>, TextAsset>, IEventRewardsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<RewardConfig> rewards;
        }
        
        protected override List<RewardConfig> ParseAsset(TextAsset textAsset)
        {
            return JsonConvert.DeserializeObject<Wrapper>(textAsset.text).rewards;
        }
    }
}