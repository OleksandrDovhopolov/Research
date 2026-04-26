using System;
using System.Collections.Generic;
using CoreResources;
using Rewards;
using UnityEngine;

namespace BattlePass
{
    public interface IBattlePassOptimisticRewardApplier
    {
        void Apply(IReadOnlyList<BattlePassGrantedRewardCell> grantedRewards);
    }

    public interface IOptimisticResourceApplyService
    {
        void Apply(IReadOnlyDictionary<string, int> resourceDeltas);
    }

    public sealed class BattlePassOptimisticRewardApplier : IBattlePassOptimisticRewardApplier
    {
        private readonly IOptimisticResourceApplyService _optimisticResourceApplyService;
        private readonly IRewardSpecProvider _rewardSpecProvider;

        public BattlePassOptimisticRewardApplier(
            IRewardSpecProvider rewardSpecProvider,
            IOptimisticResourceApplyService optimisticResourceApplyService)
        {
            _rewardSpecProvider = rewardSpecProvider ?? throw new ArgumentNullException(nameof(rewardSpecProvider));
            _optimisticResourceApplyService = optimisticResourceApplyService ?? throw new ArgumentNullException(nameof(optimisticResourceApplyService));
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

                if (!_rewardSpecProvider.TryGet(grantedReward.RewardId, out var rewardSpec) || rewardSpec == null)
                {
                    Debug.LogError($"[BattlePassOptimisticRewardApplier] Unknown rewardId '{grantedReward.RewardId}' was skipped.");
                    continue;
                }

                if (rewardSpec.Resources == null || rewardSpec.Resources.Count == 0)
                {
                    Debug.LogError($"[BattlePassOptimisticRewardApplier] Reward spec '{grantedReward.RewardId}' has no resources and was skipped.");
                    continue;
                }

                for (var resourceIndex = 0; resourceIndex < rewardSpec.Resources.Count; resourceIndex++)
                {
                    var rewardResource = rewardSpec.Resources[resourceIndex];
                    if (rewardResource == null || rewardResource.Amount <= 0)
                    {
                        continue;
                    }

                    if (rewardResource.Kind != RewardKind.Resource)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(rewardResource.ResourceId))
                    {
                        Debug.LogError($"[BattlePassOptimisticRewardApplier] Reward spec '{grantedReward.RewardId}' contains resource with empty id.");
                        continue;
                    }

                    aggregatedResourceDeltas.TryGetValue(rewardResource.ResourceId, out var currentAmount);
                    aggregatedResourceDeltas[rewardResource.ResourceId] = currentAmount + rewardResource.Amount;
                }
            }

            if (aggregatedResourceDeltas.Count == 0)
            {
                return;
            }

            _optimisticResourceApplyService.Apply(aggregatedResourceDeltas);
        }
    }

    public sealed class ResourceManagerOptimisticResourceApplyService : IOptimisticResourceApplyService
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
