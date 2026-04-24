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
        public BattlePassUserState(string seasonId, int level, int xp, BattlePassPassType passType)
        {
            SeasonId = seasonId ?? string.Empty;
            Level = Math.Max(0, level);
            Xp = Math.Max(0, xp);
            PassType = passType;
        }

        public string SeasonId { get; }
        public int Level { get; }
        public int Xp { get; }
        public BattlePassPassType PassType { get; }
    }

    public sealed class BattlePassLevel
    {
        public BattlePassLevel(
            int level,
            int xpRequired,
            IReadOnlyList<BattlePassRewardRef> defaultRewards,
            IReadOnlyList<BattlePassRewardRef> premiumRewards)
        {
            Level = Math.Max(0, level);
            XpRequired = Math.Max(0, xpRequired);
            DefaultRewards = defaultRewards ?? Array.Empty<BattlePassRewardRef>();
            PremiumRewards = premiumRewards ?? Array.Empty<BattlePassRewardRef>();
        }

        public int Level { get; }
        public int XpRequired { get; }
        public IReadOnlyList<BattlePassRewardRef> DefaultRewards { get; }
        public IReadOnlyList<BattlePassRewardRef> PremiumRewards { get; }
    }

    public sealed class BattlePassRewardRef
    {
        public BattlePassRewardRef(string rewardId)
        {
            RewardId = rewardId ?? string.Empty;
        }

        public string RewardId { get; }
    }
}
