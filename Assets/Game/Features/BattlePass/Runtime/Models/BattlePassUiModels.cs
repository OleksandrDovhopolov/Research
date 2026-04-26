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
            IReadOnlyList<BattlePassRewardUiModel> defaultRewards,
            IReadOnlyList<BattlePassRewardUiModel> premiumRewards)
        {
            Title = title ?? string.Empty;
            CurrentLevel = Math.Max(0, currentLevel);
            CurrentXp = Math.Max(0, currentXp);
            PassType = passType;
            PremiumProductId = premiumProductId ?? string.Empty;
            PlatinumProductId = platinumProductId ?? string.Empty;
            DefaultRewards = defaultRewards ?? Array.Empty<BattlePassRewardUiModel>();
            PremiumRewards = premiumRewards ?? Array.Empty<BattlePassRewardUiModel>();
        }

        public string Title { get; }
        public int CurrentLevel { get; }
        public int CurrentXp { get; }
        public BattlePassPassType PassType { get; }
        public string PremiumProductId { get; }
        public string PlatinumProductId { get; }
        public IReadOnlyList<BattlePassRewardUiModel> DefaultRewards { get; }
        public IReadOnlyList<BattlePassRewardUiModel> PremiumRewards { get; }
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
        public BattlePassRewardUiModel(
            int level,
            BattlePassRewardTrack rewardTrack,
            string rewardId,
            Sprite icon,
            int amount,
            bool isClaimed,
            bool isClaimable,
            bool isLocked,
            bool isPremiumTrack)
        {
            Level = Math.Max(0, level);
            RewardTrack = rewardTrack;
            RewardId = rewardId ?? string.Empty;
            Icon = icon;
            Amount = Math.Max(0, amount);
            IsClaimed = isClaimed;
            IsClaimable = isClaimable;
            IsLocked = isLocked;
            IsPremiumTrack = isPremiumTrack;
        }

        public int Level { get; }
        public BattlePassRewardTrack RewardTrack { get; }
        public string RewardId { get; }
        public Sprite Icon { get; }
        public int Amount { get; }
        public bool IsClaimed { get; }
        public bool IsClaimable { get; }
        public bool IsLocked { get; }
        public bool IsPremiumTrack { get; }
    }
}
