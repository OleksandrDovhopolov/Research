using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FortuneWheel
{
    public interface IFortuneWheelServerService
    {
        UniTask<FortuneWheelDataServerItem> GetDataSync(CancellationToken ct = default);
        UniTask<IReadOnlyList<FortuneWheelRewardServerItem>> GetRewardsAsync(CancellationToken ct = default);
        UniTask<FortuneWheelSpinResult> SpinAsync(CancellationToken ct = default);
    }

    public sealed class FortuneWheelRewardServerItem
    {
        public string RewardId { get; }

        public FortuneWheelRewardServerItem(string rewardId)
        {
            RewardId = rewardId;
        }
    }

    public sealed class FortuneWheelDataServerItem
    {
        public int AvailableSpins { get; }
        public long UpdatedAt { get; }
        public long NextUpdateAt { get; }

        public FortuneWheelDataServerItem(int availableSpins, long updatedAt, long nextUpdateAt)
        {
            AvailableSpins = availableSpins;
            UpdatedAt = updatedAt;
            NextUpdateAt = nextUpdateAt;
        }
    }
    
    public sealed class FortuneWheelSpinResult
    {
        public string RewardId { get; }
        public int AvailableSpins { get; }
        public long UpdatedAt { get; }
        public long NextUpdateAt { get; }

        public FortuneWheelSpinResult(
            string rewardId,
            int availableSpins,
            long updatedAt,
            long nextUpdateAt)
        {
            RewardId = rewardId;
            AvailableSpins = availableSpins;
            UpdatedAt = updatedAt;
            NextUpdateAt = nextUpdateAt;
        }
    }
}
