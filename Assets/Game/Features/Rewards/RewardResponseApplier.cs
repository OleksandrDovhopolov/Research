using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rewards
{
    public sealed class RewardResponseApplier : IRewardResponseApplier
    {
        private readonly IPlayerStateSnapshotApplier _snapshotApplier;

        public RewardResponseApplier(IPlayerStateSnapshotApplier snapshotApplier)
        {
            _snapshotApplier = snapshotApplier ?? throw new ArgumentNullException(nameof(snapshotApplier));
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

            await _snapshotApplier.ApplyAsync(response.PlayerState, ct);

            return true;
        }
    }
}
