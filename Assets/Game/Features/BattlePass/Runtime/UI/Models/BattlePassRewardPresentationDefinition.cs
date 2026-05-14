using System;
using UnityEngine;

namespace BattlePass
{
    public sealed class BattlePassRewardPresentationDefinition
    {
        public BattlePassRewardPresentationDefinition(string rewardId, Sprite icon, int displayAmount)
        {
            RewardId = rewardId ?? string.Empty;
            Icon = icon;
            DisplayAmount = Math.Max(0, displayAmount);
        }

        public string RewardId { get; }
        public Sprite Icon { get; }
        public int DisplayAmount { get; }
    }
}
