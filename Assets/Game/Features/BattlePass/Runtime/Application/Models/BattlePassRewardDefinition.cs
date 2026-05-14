using System;
using System.Collections.Generic;

namespace BattlePass
{
    public sealed class BattlePassRewardDefinition
    {
        public BattlePassRewardDefinition(
            string rewardId,
            int displayAmount,
            IReadOnlyDictionary<string, int> resourceDeltas)
        {
            RewardId = rewardId ?? string.Empty;
            DisplayAmount = Math.Max(0, displayAmount);
            ResourceDeltas = resourceDeltas ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public string RewardId { get; }
        public int DisplayAmount { get; }
        public IReadOnlyDictionary<string, int> ResourceDeltas { get; }
    }
}
