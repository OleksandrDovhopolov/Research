using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardGrantService
    {
        UniTask<bool> TryGrantAsync(string rewardId, string rewardSource = RewardSources.Client, CancellationToken ct = default);
        UniTask<RewardGrantDetailedResult> TryGrantDetailedAsync(string rewardId, string rewardSource = RewardSources.Client, CancellationToken ct = default);
    }
}
