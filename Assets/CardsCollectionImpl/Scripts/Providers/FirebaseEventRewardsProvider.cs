using System;
using System.Collections;
using System.Collections.Generic;
using CardCollection.Core;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class FirebaseEventRewardsProvider : BaseAddressablesProvider<List<RewardConfig>, CardEventRewardsConfigSO>, IEventRewardsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<RewardConfig> rewards;
        }
        
        protected override List<RewardConfig> ParseAsset(CardEventRewardsConfigSO rewardsConfigSo)
        {
            var result = rewardsConfigSo.ToString();
            
            
            return JsonConvert.DeserializeObject<Wrapper>(result).rewards;
        }
    }
}