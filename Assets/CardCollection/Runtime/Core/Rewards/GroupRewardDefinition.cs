using System;
using UnityEngine;

namespace CardCollection.Core
{
    //TODO rename CollectionCompletionRewardConfig. can cause data clear in CardCollectionRewardsConfigSO
    [Serializable]
    public struct GroupRewardDefinition
    {
        public string GroupId;
        public Sprite Icon;
        public string RewardId;
        public int Amount;
    }
}