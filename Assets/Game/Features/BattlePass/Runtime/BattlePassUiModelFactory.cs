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

            var userState = snapshot.UserState;
            var products = snapshot.Products;
            var claimedRewardKeys = BuildClaimedRewardKeys(userState);
            var claimableRewardKeys = BuildClaimableRewardKeys(userState);

            var defaultRewards = BuildRewards(
                orderedLevels,
                isPremiumTrack: false,
                userState,
                claimedRewardKeys,
                claimableRewardKeys);
            var premiumRewards = BuildRewards(
                orderedLevels,
                isPremiumTrack: true,
                userState,
                claimedRewardKeys,
                claimableRewardKeys);

            return new BattlePassWindowUiModel(
                snapshot.Season.Title,
                userState?.Level ?? 0,
                userState?.Xp ?? 0,
                userState?.PassType ?? BattlePassPassType.Unknown,
                products?.PremiumProductId ?? string.Empty,
                products?.PlatinumProductId ?? string.Empty,
                defaultRewards,
                premiumRewards);
        }

        private IReadOnlyList<BattlePassRewardUiModel> BuildRewards(
            IReadOnlyList<BattlePassLevel> levels,
            bool isPremiumTrack,
            BattlePassUserState userState,
            HashSet<string> claimedRewardKeys,
            HashSet<string> claimableRewardKeys)
        {
            var rewardModels = new List<BattlePassRewardUiModel>();

            if (levels == null)
            {
                return rewardModels;
            }

            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var reward = isPremiumTrack ? level.PremiumReward : level.DefaultReward;
                if (reward == null || string.IsNullOrWhiteSpace(reward.RewardId))
                {
                    continue;
                }

                if (!_rewardSpecProvider.TryGet(reward.RewardId, out var spec) || spec == null)
                {
                    Debug.LogWarning($"[BattlePassUiModelFactory] Unknown reward id '{reward.RewardId}'. Reward skipped.");
                    continue;
                }

                var rewardTrack = isPremiumTrack ? BattlePassRewardTrack.Premium : BattlePassRewardTrack.Default;
                var rewardCellKey = BuildRewardCellKey(level.Level, rewardTrack);
                var isClaimed = claimedRewardKeys.Contains(rewardCellKey);
                var isClaimable = !isClaimed && claimableRewardKeys.Contains(rewardCellKey);
                var isLocked = !isClaimed && ResolveLockedState(isPremiumTrack, userState);

                rewardModels.Add(new BattlePassRewardUiModel(
                    level.Level,
                    rewardTrack,
                    reward.RewardId,
                    spec.Icon,
                    Mathf.Max(0, spec.TotalAmountForUi),
                    isClaimed,
                    isClaimable,
                    isLocked,
                    isPremiumTrack));
            }

            return rewardModels;
        }

        private static HashSet<string> BuildClaimedRewardKeys(BattlePassUserState userState)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (userState?.ClaimedRewards == null)
            {
                return keys;
            }

            foreach (var claimedReward in userState.ClaimedRewards)
            {
                if (claimedReward == null || claimedReward.Level <= 0)
                {
                    continue;
                }

                keys.Add(BuildRewardCellKey(claimedReward.Level, claimedReward.RewardTrack));
            }

            return keys;
        }

        private static HashSet<string> BuildClaimableRewardKeys(BattlePassUserState userState)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (userState?.ClaimableRewards == null)
            {
                return keys;
            }

            foreach (var claimableReward in userState.ClaimableRewards)
            {
                if (claimableReward == null || claimableReward.Level <= 0)
                {
                    continue;
                }

                keys.Add(BuildRewardCellKey(claimableReward.Level, claimableReward.RewardTrack));
            }

            return keys;
        }

        private static string BuildRewardCellKey(int level, BattlePassRewardTrack rewardTrack)
        {
            return $"{level}:{rewardTrack}";
        }

        private static bool ResolveLockedState(bool isPremiumTrack, BattlePassUserState userState)
        {
            if (!isPremiumTrack)
            {
                return false;
            }

            return userState?.PassType is not BattlePassPassType.Premium and not BattlePassPassType.Platinum;
        }
    }
}
