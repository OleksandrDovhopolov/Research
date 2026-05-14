using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattlePass
{
    public sealed class BattlePassRewardDefinition
    {
        public BattlePassRewardDefinition(
            string rewardId,
            Sprite icon,
            int displayAmount,
            IReadOnlyDictionary<string, int> resourceDeltas)
        {
            RewardId = rewardId ?? string.Empty;
            Icon = icon;
            DisplayAmount = Math.Max(0, displayAmount);
            ResourceDeltas = resourceDeltas ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public string RewardId { get; }
        public Sprite Icon { get; }
        public int DisplayAmount { get; }
        public IReadOnlyDictionary<string, int> ResourceDeltas { get; }
    }
}
