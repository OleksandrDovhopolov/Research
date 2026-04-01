using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rewards
{
    [Serializable]
    public class RewardSpec
    {
        public string RewardId;
        public Sprite Icon;
        public int TotalAmountForUi;
        public List<RewardSpecResource> Resources;
    }
}