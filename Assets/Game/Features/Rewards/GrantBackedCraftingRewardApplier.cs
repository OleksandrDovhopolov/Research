using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Crafting;
using UnityEngine;

namespace Rewards
{
    public sealed class GrantBackedCraftingRewardApplier : ICraftingRewardApplier
    {
        private readonly IRewardGrantService _rewardGrantService;

        public GrantBackedCraftingRewardApplier(IRewardGrantService rewardGrantService)
        {
            _rewardGrantService = rewardGrantService ?? throw new ArgumentNullException(nameof(rewardGrantService));
        }

        public async UniTask ApplyAsync(string outputItemId, int outputCount, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(outputItemId))
                throw new InvalidOperationException("Crafting reward grant failed: output item id is empty.");

            if (outputCount != 1)
            {
                throw new InvalidOperationException(
                    $"Crafting reward grant failed: output count '{outputCount}' is not supported for item '{outputItemId}'.");
            }

            Debug.LogWarning(
                $"[CraftingRewardApplier] Granting crafted reward. RewardId='{outputItemId}', RewardSource='{RewardSources.Crafting}'.");

            var result = await _rewardGrantService.TryGrantDetailedAsync(outputItemId, RewardSources.Crafting, ct);
            if (!result.Success)
            {
                Debug.LogWarning(
                    $"[CraftingRewardApplier] Crafted reward grant failed. RewardId='{outputItemId}', RewardSource='{RewardSources.Crafting}', FailureType={result.FailureType}, ErrorCode='{result.ErrorCode ?? "<none>"}', Message='{result.ErrorMessage ?? "<none>"}'.");
                throw new InvalidOperationException(
                    $"Crafting reward grant failed for '{outputItemId}'. FailureType={result.FailureType}, ErrorCode={result.ErrorCode ?? "<none>"}, Message={result.ErrorMessage ?? "<none>"}.");
            }

            Debug.LogWarning(
                $"[CraftingRewardApplier] Crafted reward granted. RewardId='{result.RewardId}', RewardSource='{RewardSources.Crafting}'.");
        }
    }
}
