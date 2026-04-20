using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rewards
{
    public sealed class GameRewardGrantService : IRewardGrantService
    {
        private readonly IReadOnlyList<IRewardHandler> _handlers;
        private readonly IRewardSpecProvider _rewardSpecProvider;

        public GameRewardGrantService(IReadOnlyList<IRewardHandler> handlers, IRewardSpecProvider rewardSpecProvider)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            _rewardSpecProvider = rewardSpecProvider ?? throw new ArgumentNullException(nameof(rewardSpecProvider));
        }

        public UniTask<bool> TryGrantAsync(string rewardId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                Debug.LogWarning("[Rewards] Reward id is empty.");
                return UniTask.FromResult(false);
            }

            if (!_rewardSpecProvider.TryGet(rewardId, out var rewardSpec) || rewardSpec == null)
            {
                Debug.LogWarning($"[Rewards] Unknown reward id: {rewardId}");
                return UniTask.FromResult(false);
            }

            if (!TryBuildRequests(rewardSpec, out var requests))
            {
                Debug.LogWarning($"[Rewards] RewardSpec '{rewardId}' has no valid resources.");
                return UniTask.FromResult(false);
            }

            return TryGrantAsync(requests, ct);
        }

        public async UniTask<bool> TryGrantAsync(List<RewardGrantRequest> rewards, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (rewards == null || rewards.Count == 0)
            {
                Debug.LogWarning("Failed to add reward list. List is null or empty");
                return false;
            }

            var allSuccess = true;
            foreach (var request in rewards)
            {
                ct.ThrowIfCancellationRequested();
                if (!IsValidRequest(request))
                {
                    Debug.LogWarning("[Rewards] Invalid reward request. Reward will be skipped.");
                    allSuccess = false;
                    continue;
                }

                var handler = _handlers.FirstOrDefault(x => x != null && x.CanHandle(request));
                if (handler == null)
                {
                    Debug.LogWarning($"[Rewards] Unsupported reward. Id={request.RewardId}, Kind={request.Kind}");
                    allSuccess = false;
                    continue;
                }

                try
                {
                    await handler.HandleAsync(request, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Rewards] Failed to grant reward. Id={request.RewardId}, Kind={request.Kind}. Error={e}");
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        public UniTask<bool> TryApplyGrantResponseAsync(GrantRewardResponse grantResponse, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning("[Rewards] Applying server grant response is not supported in GameRewardGrantService.");
            return UniTask.FromResult(false);
        }

        public UniTask<RewardGrantDetailedResult> TryGrantDetailedAsync(string rewardId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Debug.LogWarning("[Rewards] Detailed grant is not supported in GameRewardGrantService.");
            var result = RewardGrantDetailedResult.BuildFailure(
                rewardId,
                RewardGrantFailureType.Unknown,
                "UNSUPPORTED",
                "Detailed grant is not supported in GameRewardGrantService.");
            return UniTask.FromResult(result);
        }

        private static bool IsValidRequest(RewardGrantRequest request)
        {
            if (request == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.RewardId))
            {
                return false;
            }

            if (request.Kind == RewardKind.Unknown)
            {
                return false;
            }

            if (RequiresPositiveAmount(request.Kind) && request.Amount <= 0)
            {
                return false;
            }

            return true;
        }

        private static bool RequiresPositiveAmount(RewardKind kind)
        {
            return kind == RewardKind.Resource || kind == RewardKind.InventoryItem;
        }

        private static bool TryBuildRequests(RewardSpec rewardSpec, out List<RewardGrantRequest> requests)
        {
            requests = null;
            if (rewardSpec?.Resources == null || rewardSpec.Resources.Count == 0)
            {
                return false;
            }

            var builtRequests = new List<RewardGrantRequest>(rewardSpec.Resources.Count);
            foreach (var resource in rewardSpec.Resources)
            {
                if (resource == null ||
                    string.IsNullOrWhiteSpace(resource.ResourceId) ||
                    resource.Amount <= 0 ||
                    resource.Kind == RewardKind.Unknown)
                {
                    continue;
                }

                builtRequests.Add(new RewardGrantRequest(resource.ResourceId, resource.Kind, resource.Amount, resource.Category));
            }

            if (builtRequests.Count == 0)
            {
                return false;
            }

            requests = builtRequests;
            return true;
        }
    }
}
