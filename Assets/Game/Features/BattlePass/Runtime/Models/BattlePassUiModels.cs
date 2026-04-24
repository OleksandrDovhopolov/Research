using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattlePass
{
    public sealed class BattlePassWindowUiModel
    {
        public BattlePassWindowUiModel(
            string title,
            int currentLevel,
            int currentXp,
            BattlePassPassType passType,
            string premiumProductId,
            string platinumProductId,
            IReadOnlyList<BattlePassTrackLevelUiModel> defaultTrackLevels,
            IReadOnlyList<BattlePassTrackLevelUiModel> premiumTrackLevels)
        {
            Title = title ?? string.Empty;
            CurrentLevel = Math.Max(0, currentLevel);
            CurrentXp = Math.Max(0, currentXp);
            PassType = passType;
            PremiumProductId = premiumProductId ?? string.Empty;
            PlatinumProductId = platinumProductId ?? string.Empty;
            DefaultTrackLevels = defaultTrackLevels ?? Array.Empty<BattlePassTrackLevelUiModel>();
            PremiumTrackLevels = premiumTrackLevels ?? Array.Empty<BattlePassTrackLevelUiModel>();
        }

        public string Title { get; }
        public int CurrentLevel { get; }
        public int CurrentXp { get; }
        public BattlePassPassType PassType { get; }
        public string PremiumProductId { get; }
        public string PlatinumProductId { get; }
        public IReadOnlyList<BattlePassTrackLevelUiModel> DefaultTrackLevels { get; }
        public IReadOnlyList<BattlePassTrackLevelUiModel> PremiumTrackLevels { get; }
    }

    public sealed class BattlePassTrackLevelUiModel
    {
        public BattlePassTrackLevelUiModel(int level, IReadOnlyList<BattlePassRewardUiModel> rewards)
        {
            Level = Math.Max(0, level);
            Rewards = rewards ?? Array.Empty<BattlePassRewardUiModel>();
        }

        public int Level { get; }
        public IReadOnlyList<BattlePassRewardUiModel> Rewards { get; }
    }

    public sealed class BattlePassRewardUiModel
    {
        public BattlePassRewardUiModel(string rewardId, Sprite icon, int amount)
        {
            RewardId = rewardId ?? string.Empty;
            Icon = icon;
            Amount = Math.Max(0, amount);
        }

        public string RewardId { get; }
        public Sprite Icon { get; }
        public int Amount { get; }
    }
}
