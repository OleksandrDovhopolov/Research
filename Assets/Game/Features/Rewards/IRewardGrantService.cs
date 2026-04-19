using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardGrantService
    {
        UniTask<bool> TryGrantAsync(string rewardId, CancellationToken ct = default);
        UniTask<bool> TryGrantAsync(List<RewardGrantRequest> rewards, CancellationToken ct = default);
    }
}
