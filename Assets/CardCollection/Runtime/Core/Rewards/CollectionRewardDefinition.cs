using System;
using UnityEngine;

namespace CardCollection.Core
{
    //TODO rename FullCollectionRewardConfig. can cause data clear in CardCollectionRewardsConfigSO
    [Serializable]
    public struct CollectionRewardDefinition
    {
        public Sprite Icon;
        public string RewardId;
        public int Amount;
    }
}
