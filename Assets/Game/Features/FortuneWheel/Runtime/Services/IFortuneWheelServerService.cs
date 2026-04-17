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
        public Sprite RewardSprite { get; }
        public int RewardAmount { get; }
        public string RewardResourceId { get; }
        public int AvailableSpins { get; }
        public long UpdatedAt { get; }
        public long NextUpdateAt { get; }

        public FortuneWheelSpinResult(
            string rewardId,
            Sprite rewardSprite,
            int rewardAmount,
            string rewardResourceId,
            int availableSpins,
            long updatedAt,
            long nextUpdateAt)
        {
            RewardId = rewardId;
            RewardSprite = rewardSprite;
            RewardAmount = rewardAmount;
            RewardResourceId = rewardResourceId;
            AvailableSpins = availableSpins;
            UpdatedAt = updatedAt;
            NextUpdateAt = nextUpdateAt;
        }
    }
}
