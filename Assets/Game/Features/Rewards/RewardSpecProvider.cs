using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rewards
{
    public sealed class RewardSpecProvider : IRewardSpecProvider
    {
        private const string DefaultRewardId = "default";
        private readonly Dictionary<string, RewardSpec> _rewardSpecsById;

        public RewardSpecProvider(RewardSpecsConfigSO config) : this(config?.RewardSpecs)
        {
        }

        public RewardSpecProvider(IReadOnlyList<RewardSpec> rewardSpecs)
        {
            _rewardSpecsById = new Dictionary<string, RewardSpec>(StringComparer.Ordinal);
            if (rewardSpecs == null || rewardSpecs.Count == 0)
            {
                return;
            }

            foreach (var rewardSpec in rewardSpecs)
            {
                if (string.IsNullOrWhiteSpace(rewardSpec.RewardId))
                {
                    continue;
                }

                _rewardSpecsById[rewardSpec.RewardId] = rewardSpec;
            }
        }

        public bool TryGet(string rewardId, out RewardSpec spec)
        {
            spec = null;
            if (!string.IsNullOrWhiteSpace(rewardId) && _rewardSpecsById.TryGetValue(rewardId, out spec))
            {
                return true;
            }

            Debug.LogWarning($"[RewardSpecProvider] failed to find reward with ID {rewardId}. Default reward returned");
            return _rewardSpecsById.TryGetValue(DefaultRewardId, out spec);
        }
    }
}
