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
            var hasLoggedMissingClaimedState = false;

            var defaultRewards = BuildRewards(
                orderedLevels,
                isPremiumTrack: false,
                userState,
                ref hasLoggedMissingClaimedState);
            var premiumRewards = BuildRewards(
                orderedLevels,
                isPremiumTrack: true,
                userState,
                ref hasLoggedMissingClaimedState);

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
            ref bool hasLoggedMissingClaimedState)
        {
            var rewardModels = new List<BattlePassRewardUiModel>();

            if (levels == null)
            {
                return rewardModels;
            }

            for (var i = 0; i < levels.Count; i++)
            {
                var rewards = isPremiumTrack ? levels[i].PremiumRewards : levels[i].DefaultRewards;
                if (rewards == null)
                {
                    continue;
                }

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
                        Mathf.Max(0, spec.TotalAmountForUi),
                        ResolveClaimedState(reward.RewardId, ref hasLoggedMissingClaimedState),
                        ResolveLockedState(isPremiumTrack, userState),
                        isPremiumTrack));
                }
            }

            return rewardModels;
        }

        private static bool ResolveClaimedState(string rewardId, ref bool hasLoggedMissingClaimedState)
        {
            if (!hasLoggedMissingClaimedState)
            {
                Debug.LogError($"[BattlePassUiModelFactory] Claimed reward state is unavailable. Server snapshot does not contain claimedRewards data. First unresolved rewardId='{rewardId}'.");
                hasLoggedMissingClaimedState = true;
            }

            return false;
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
