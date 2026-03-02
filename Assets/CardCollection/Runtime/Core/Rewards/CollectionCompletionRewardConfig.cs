using System;
using UnityEngine;

namespace CardCollection.Core
{
    [Serializable]
    public struct CollectionCompletionRewardConfig
    {
        public string GroupId;
        public Sprite Icon;
        public string RewardId;
        public int Amount;
    }
}