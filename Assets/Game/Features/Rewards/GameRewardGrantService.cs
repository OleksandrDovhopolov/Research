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

        public GameRewardGrantService(IReadOnlyList<IRewardHandler> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
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
    }
}
