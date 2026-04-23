using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rewards
{
    public sealed class RewardResponseApplier : IRewardResponseApplier
    {
        private readonly IReadOnlyList<IPlayerStateSnapshotHandler> _snapshotHandlers;

        public RewardResponseApplier(IEnumerable<IPlayerStateSnapshotHandler> snapshotHandlers)
        {
            if (snapshotHandlers == null)
            {
                throw new ArgumentNullException(nameof(snapshotHandlers));
            }

            _snapshotHandlers = snapshotHandlers.Where(handler => handler != null).ToList();
        }

        public async UniTask<bool> TryApplyAsync(GrantRewardResponse response, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (response == null)
            {
                Debug.LogWarning("[Rewards] Grant response is null.");
                return false;
            }

            var rewardId = string.IsNullOrWhiteSpace(response.RewardId) ? "<unknown>" : response.RewardId;
            if (!response.Success)
            {
                Debug.LogWarning(
                    $"[Rewards] Grant rejected. RewardId={rewardId}, Code={response.ErrorCode ?? "<none>"}, Message={response.ErrorMessage ?? "<none>"}");
                return false;
            }

            if (response.PlayerState == null)
            {
                Debug.LogWarning($"[Rewards] Grant response has no playerState. RewardId={rewardId}");
                return false;
            }

            foreach (var snapshotHandler in _snapshotHandlers)
            {
                ct.ThrowIfCancellationRequested();
                await snapshotHandler.ApplyAsync(response.PlayerState, ct);
            }

            return true;
        }
    }
}
