using System;
using System.Collections.Generic;
using System.Linq;
using Rewards;
using UnityEngine;

namespace BattlePass
{
    public sealed class BattlePassUiModelFactory
    {
        private readonly IRewardSpecProvider _rewardSpecProvider;

        public BattlePassUiModelFactory(IRewardSpecProvider rewardSpecProvider)
        {
            _rewardSpecProvider = rewardSpecProvider ?? throw new ArgumentNullException(nameof(rewardSpecProvider));
        }

        public BattlePassWindowUiModel Create(BattlePassSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Season == null)
            {
                return null;
            }

            var orderedLevels = snapshot.Levels?
                .Where(level => level != null)
                .OrderBy(level => level.Level)
                .ToArray() ?? Array.Empty<BattlePassLevel>();

            var defaultTrackLevels = orderedLevels
                .Select(level => BuildTrackLevel(level.Level, level.DefaultRewards))
                .ToArray();

            var premiumTrackLevels = orderedLevels
                .Select(level => BuildTrackLevel(level.Level, level.PremiumRewards))
                .ToArray();

            var userState = snapshot.UserState;
            var products = snapshot.Products;

            return new BattlePassWindowUiModel(
                snapshot.Season.Title,
                userState?.Level ?? 0,
                userState?.Xp ?? 0,
                userState?.PassType ?? BattlePassPassType.Unknown,
                products?.PremiumProductId ?? string.Empty,
                products?.PlatinumProductId ?? string.Empty,
                defaultTrackLevels,
                premiumTrackLevels);
        }

        private BattlePassTrackLevelUiModel BuildTrackLevel(int level, IReadOnlyList<BattlePassRewardRef> rewards)
        {
            var rewardModels = new List<BattlePassRewardUiModel>();

            if (rewards != null)
            {
                foreach (var reward in rewards)
                {
                    if (reward == null || string.IsNullOrWhiteSpace(reward.RewardId))
                    {
                        continue;
                    }

                    if (!_rewardSpecProvider.TryGet(reward.RewardId, out var spec) || spec == null)
                    {
                        Debug.LogWarning($"[BattlePassUiModelFactory] Unknown reward id '{reward.RewardId}'. Reward skipped.");
                        continue;
                    }

                    rewardModels.Add(new BattlePassRewardUiModel(
                        reward.RewardId,
                        spec.Icon,
                        Mathf.Max(0, spec.TotalAmountForUi)));
                }
            }

            return new BattlePassTrackLevelUiModel(level, rewardModels);
        }
    }
}
