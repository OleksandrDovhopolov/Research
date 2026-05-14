using System;
using System.Collections.Generic;
using CoreResources;
using Rewards;
using UnityEngine;

namespace BattlePass
{
    public sealed class RewardSpecBattlePassRewardCatalog : IBattlePassRewardCatalog
    {
        private readonly IRewardSpecProvider _rewardSpecProvider;

        public RewardSpecBattlePassRewardCatalog(IRewardSpecProvider rewardSpecProvider)
        {
            _rewardSpecProvider = rewardSpecProvider ?? throw new ArgumentNullException(nameof(rewardSpecProvider));
        }

        public bool TryGet(string rewardId, out BattlePassRewardDefinition rewardDefinition)
        {
            rewardDefinition = null;
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            if (!_rewardSpecProvider.TryGet(rewardId, out var rewardSpec) || rewardSpec == null)
            {
                return false;
            }

            var resourceDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (rewardSpec.Resources != null)
            {
                for (var i = 0; i < rewardSpec.Resources.Count; i++)
                {
                    var rewardResource = rewardSpec.Resources[i];
                    if (rewardResource == null ||
                        rewardResource.Kind != RewardKind.Resource ||
                        rewardResource.Amount <= 0 ||
                        string.IsNullOrWhiteSpace(rewardResource.ResourceId))
                    {
                        continue;
                    }

                    resourceDeltas.TryGetValue(rewardResource.ResourceId, out var currentAmount);
                    resourceDeltas[rewardResource.ResourceId] = currentAmount + rewardResource.Amount;
                }
            }

            rewardDefinition = new BattlePassRewardDefinition(
                rewardId,
                rewardSpec.TotalAmountForUi,
                resourceDeltas);
            return true;
        }
    }

    public sealed class RewardSpecBattlePassRewardPresentationCatalog : IBattlePassRewardPresentationCatalog
    {
        private readonly IRewardSpecProvider _rewardSpecProvider;

        public RewardSpecBattlePassRewardPresentationCatalog(IRewardSpecProvider rewardSpecProvider)
        {
            _rewardSpecProvider = rewardSpecProvider ?? throw new ArgumentNullException(nameof(rewardSpecProvider));
        }

        public bool TryGet(string rewardId, out BattlePassRewardPresentationDefinition rewardDefinition)
        {
            rewardDefinition = null;
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            if (!_rewardSpecProvider.TryGet(rewardId, out var rewardSpec) || rewardSpec == null)
            {
                return false;
            }

            rewardDefinition = new BattlePassRewardPresentationDefinition(
                rewardId,
                rewardSpec.Icon,
                rewardSpec.TotalAmountForUi);
            return true;
        }
    }

    public sealed class ResourceManagerOptimisticResourceApplyService : IBattlePassResourceDeltaApplier
    {
        private readonly ResourceManager _resourceManager;

        public ResourceManagerOptimisticResourceApplyService(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        }

        public void Apply(IReadOnlyDictionary<string, int> resourceDeltas)
        {
            if (resourceDeltas == null || resourceDeltas.Count == 0)
            {
                return;
            }

            foreach (var resourceDelta in resourceDeltas)
            {
                if (resourceDelta.Value <= 0)
                {
                    continue;
                }

                if (!Enum.TryParse<ResourceType>(resourceDelta.Key, true, out var resourceType))
                {
                    Debug.LogError($"[ResourceManagerOptimisticResourceApplyService] Unsupported resource id '{resourceDelta.Key}' was skipped.");
                    continue;
                }

                _resourceManager.ApplyLocalDelta(resourceType, resourceDelta.Value);
            }
        }
    }
}
