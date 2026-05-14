using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattlePass
{
    public interface IBattlePassOptimisticRewardApplier
    {
        void Apply(IReadOnlyList<BattlePassGrantedRewardCell> grantedRewards);
    }

    public sealed class BattlePassOptimisticRewardApplier : IBattlePassOptimisticRewardApplier
    {
        private readonly IBattlePassResourceDeltaApplier _resourceDeltaApplier;
        private readonly IBattlePassRewardCatalog _rewardCatalog;

        public BattlePassOptimisticRewardApplier(
            IBattlePassRewardCatalog rewardCatalog,
            IBattlePassResourceDeltaApplier resourceDeltaApplier)
        {
            _rewardCatalog = rewardCatalog ?? throw new ArgumentNullException(nameof(rewardCatalog));
            _resourceDeltaApplier = resourceDeltaApplier ?? throw new ArgumentNullException(nameof(resourceDeltaApplier));
        }

        public void Apply(IReadOnlyList<BattlePassGrantedRewardCell> grantedRewards)
        {
            if (grantedRewards == null || grantedRewards.Count == 0)
            {
                return;
            }

            var aggregatedResourceDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < grantedRewards.Count; i++)
            {
                var grantedReward = grantedRewards[i];
                if (grantedReward == null || string.IsNullOrWhiteSpace(grantedReward.RewardId))
                {
                    Debug.LogError("[BattlePassOptimisticRewardApplier] Granted reward has empty rewardId and was skipped.");
                    continue;
                }

                if (!_rewardCatalog.TryGet(grantedReward.RewardId, out var rewardDefinition) || rewardDefinition == null)
                {
                    Debug.LogError($"[BattlePassOptimisticRewardApplier] Unknown rewardId '{grantedReward.RewardId}' was skipped.");
                    continue;
                }

                if (rewardDefinition.ResourceDeltas == null || rewardDefinition.ResourceDeltas.Count == 0)
                {
                    Debug.LogError($"[BattlePassOptimisticRewardApplier] Reward spec '{grantedReward.RewardId}' has no resources and was skipped.");
                    continue;
                }

                foreach (var resourceDelta in rewardDefinition.ResourceDeltas)
                {
                    if (resourceDelta.Value <= 0)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(resourceDelta.Key))
                    {
                        Debug.LogError($"[BattlePassOptimisticRewardApplier] Reward spec '{grantedReward.RewardId}' contains resource with empty id.");
                        continue;
                    }

                    aggregatedResourceDeltas.TryGetValue(resourceDelta.Key, out var currentAmount);
                    aggregatedResourceDeltas[resourceDelta.Key] = currentAmount + resourceDelta.Value;
                }
            }

            if (aggregatedResourceDeltas.Count == 0)
            {
                return;
            }

            _resourceDeltaApplier.Apply(aggregatedResourceDeltas);
        }
    }
}
