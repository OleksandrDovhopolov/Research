using System;
using System.Collections.Generic;

namespace BattlePass
{
    public enum BattlePassPassType
    {
        Unknown = 0,
        None = 1,
        Premium = 2,
        Platinum = 3
    }

    public enum BattlePassRewardTrack
    {
        Default = 0,
        Premium = 1
    }

    public sealed class BattlePassSnapshot
    {
        public BattlePassSnapshot(
            BattlePassSeason season,
            BattlePassProducts products,
            BattlePassUserState userState,
            IReadOnlyList<BattlePassLevel> levels,
            DateTimeOffset serverTimeUtc)
        {
            Season = season;
            Products = products;
            UserState = userState;
            Levels = levels ?? Array.Empty<BattlePassLevel>();
            ServerTimeUtc = serverTimeUtc;
        }

        public BattlePassSeason Season { get; }
        public BattlePassProducts Products { get; }
        public BattlePassUserState UserState { get; }
        public IReadOnlyList<BattlePassLevel> Levels { get; }
        public DateTimeOffset ServerTimeUtc { get; }
    }

    public sealed class BattlePassSeason
    {
        public BattlePassSeason(
            string id,
            string title,
            DateTimeOffset startAtUtc,
            DateTimeOffset endAtUtc,
            int maxLevel,
            string status,
            string configVersion)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            StartAtUtc = startAtUtc;
            EndAtUtc = endAtUtc;
            MaxLevel = Math.Max(0, maxLevel);
            Status = status ?? string.Empty;
            ConfigVersion = configVersion ?? string.Empty;
        }

        public string Id { get; }
        public string Title { get; }
        public DateTimeOffset StartAtUtc { get; }
        public DateTimeOffset EndAtUtc { get; }
        public int MaxLevel { get; }
        public string Status { get; }
        public string ConfigVersion { get; }
    }

    public sealed class BattlePassProducts
    {
        public BattlePassProducts(string premiumProductId, string platinumProductId)
        {
            PremiumProductId = premiumProductId ?? string.Empty;
            PlatinumProductId = platinumProductId ?? string.Empty;
        }

        public string PremiumProductId { get; }
        public string PlatinumProductId { get; }
    }

    public sealed class BattlePassUserState
    {
        public BattlePassUserState(
            string seasonId,
            int level,
            int xp,
            BattlePassPassType passType,
            IReadOnlyList<BattlePassClaimedRewardCell> claimedRewards,
            IReadOnlyList<BattlePassClaimableRewardCell> claimableRewards)
        {
            SeasonId = seasonId ?? string.Empty;
            Level = Math.Max(0, level);
            Xp = Math.Max(0, xp);
            PassType = passType;
            ClaimedRewards = claimedRewards ?? Array.Empty<BattlePassClaimedRewardCell>();
            ClaimableRewards = claimableRewards ?? Array.Empty<BattlePassClaimableRewardCell>();
        }

        public string SeasonId { get; }
        public int Level { get; }
        public int Xp { get; }
        public BattlePassPassType PassType { get; }
        public IReadOnlyList<BattlePassClaimedRewardCell> ClaimedRewards { get; }
        public IReadOnlyList<BattlePassClaimableRewardCell> ClaimableRewards { get; }
    }

    public sealed class BattlePassLevel
    {
        public BattlePassLevel(
            int level,
            int xpRequired,
            BattlePassRewardRef defaultReward,
            BattlePassRewardRef premiumReward)
        {
            Level = Math.Max(0, level);
            XpRequired = Math.Max(0, xpRequired);
            DefaultReward = defaultReward;
            PremiumReward = premiumReward;
        }

        public int Level { get; }
        public int XpRequired { get; }
        public BattlePassRewardRef DefaultReward { get; }
        public BattlePassRewardRef PremiumReward { get; }
    }

    public sealed class BattlePassRewardRef
    {
        public BattlePassRewardRef(string rewardId)
        {
            RewardId = rewardId ?? string.Empty;
        }

        public string RewardId { get; }
    }

    public sealed class BattlePassClaimedRewardCell
    {
        public BattlePassClaimedRewardCell(int level, BattlePassRewardTrack rewardTrack, DateTimeOffset claimedAtUtc)
        {
            Level = Math.Max(0, level);
            RewardTrack = rewardTrack;
            ClaimedAtUtc = claimedAtUtc;
        }

        public int Level { get; }
        public BattlePassRewardTrack RewardTrack { get; }
        public DateTimeOffset ClaimedAtUtc { get; }
    }

    public sealed class BattlePassClaimableRewardCell
    {
        public BattlePassClaimableRewardCell(int level, BattlePassRewardTrack rewardTrack, string rewardId)
        {
            Level = Math.Max(0, level);
            RewardTrack = rewardTrack;
            RewardId = rewardId ?? string.Empty;
        }

        public int Level { get; }
        public BattlePassRewardTrack RewardTrack { get; }
        public string RewardId { get; }
    }

    public sealed class BattlePassGrantedRewardCell
    {
        public BattlePassGrantedRewardCell(int level, BattlePassRewardTrack rewardTrack, string rewardId)
        {
            Level = Math.Max(0, level);
            RewardTrack = rewardTrack;
            RewardId = rewardId ?? string.Empty;
        }

        public int Level { get; }
        public BattlePassRewardTrack RewardTrack { get; }
        public string RewardId { get; }
    }

    public sealed class BattlePassClaimResult
    {
        public BattlePassClaimResult(
            bool success,
            IReadOnlyList<BattlePassGrantedRewardCell> grantedRewards,
            BattlePassUserState updatedUserState,
            string errorCode,
            string errorMessage)
        {
            Success = success;
            GrantedRewards = grantedRewards ?? Array.Empty<BattlePassGrantedRewardCell>();
            UpdatedUserState = updatedUserState;
            ErrorCode = errorCode ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool Success { get; }
        public IReadOnlyList<BattlePassGrantedRewardCell> GrantedRewards { get; }
        public BattlePassUserState UpdatedUserState { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
    }
}
