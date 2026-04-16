using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FortuneWheel
{
    public interface IFortuneWheelServerService
    {
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

    public sealed class FortuneWheelSpinResult
    {
        public string RewardId { get; }
        public int AvailableSpins { get; }
        public int NextRegenSeconds { get; }

        public FortuneWheelSpinResult(string rewardId, int availableSpins, int nextRegenSeconds)
        {
            RewardId = rewardId;
            AvailableSpins = availableSpins;
            NextRegenSeconds = nextRegenSeconds;
        }
    }
}
