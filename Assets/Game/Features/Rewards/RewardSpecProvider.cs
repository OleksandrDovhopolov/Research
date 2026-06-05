using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rewards
{
    public sealed class RewardSpecProvider : IRewardSpecProvider
    {
        private readonly Dictionary<string, RewardSpec> _rewardSpecsById;
        private readonly Dictionary<string, Sprite> _iconsByResourceId;

        public RewardSpecProvider(RewardSpecsConfigSO config) : this(config?.RewardSpecs)
        {
        }

        public RewardSpecProvider(IReadOnlyList<RewardSpec> rewardSpecs)
        {
            _rewardSpecsById = new Dictionary<string, RewardSpec>(StringComparer.Ordinal);
            _iconsByResourceId = new Dictionary<string, Sprite>(StringComparer.Ordinal);
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

                // Build a secondary index resourceId -> icon so UI (inventory, etc.)
                // can resolve icons by item id without scanning all specs at runtime.
                if (rewardSpec.Resources == null)
                {
                    continue;
                }

                foreach (var resource in rewardSpec.Resources)
                {
                    if (resource == null ||
                        string.IsNullOrWhiteSpace(resource.ResourceId) ||
                        resource.Icon == null)
                    {
                        continue;
                    }

                    // Первый non-null Icon побеждает. Если у одного и того же ResourceId
                    // встречается несколько разных Icon в разных спеках — это ошибка данных,
                    // лечится в SO, а не в коде.
                    if (!_iconsByResourceId.ContainsKey(resource.ResourceId))
                    {
                        _iconsByResourceId[resource.ResourceId] = resource.Icon;
                    }
                }
            }
        }

        public bool TryGet(string rewardId, out RewardSpec spec)
        {
            spec = null;
            if (!string.IsNullOrWhiteSpace(rewardId) && _rewardSpecsById.TryGetValue(rewardId, out spec))
            {
                return true;
            }

            Debug.LogError($"[RewardSpecProvider] failed to find reward with ID {rewardId}. Default reward returned");
            return false;
        }

        public bool TryGetResourceIcon(string resourceId, out Sprite icon)
        {
            icon = null;
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                return false;
            }

            // 1) точный match по ResourceId в любой из RewardSpec.Resources
            if (_iconsByResourceId.TryGetValue(resourceId, out icon) && icon != null)
            {
                return true;
            }

            // 2) запасной путь: вдруг этот id — это RewardId, не ResourceId.
            //    Возвращаем top-level RewardSpec.Icon если есть.
            if (_rewardSpecsById.TryGetValue(resourceId, out var spec) && spec?.Icon != null)
            {
                icon = spec.Icon;
                return true;
            }

            return false;
        }
    }
}
