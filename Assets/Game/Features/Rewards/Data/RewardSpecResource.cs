using System;
using UnityEngine;

namespace Rewards
{
    [Serializable]
    public class RewardSpecResource
    {
        public string ResourceId;
        public int Amount;
        public string Category;
        public Sprite Icon;
    }
}