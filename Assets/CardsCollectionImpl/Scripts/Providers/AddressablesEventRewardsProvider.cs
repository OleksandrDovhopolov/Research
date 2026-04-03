using System;
using System.Collections.Generic;
using CardCollection.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class AddressablesEventRewardsProvider : BaseAddressablesProvider<List<RewardConfig>, CardEventRewardsConfigSO>, IEventRewardsConfigProvider
    {
        [Serializable]
        private class Wrapper
        {
            public List<RewardConfig> rewards;
        }
        
        protected override List<RewardConfig> ParseAsset(CardEventRewardsConfigSO rewardsConfigSo)
        {
            return JsonConvert.DeserializeObject<Wrapper>(rewardsConfigSo.ToString()).rewards;
        }
    }
}