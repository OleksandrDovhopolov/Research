using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardGrantService
    {
        UniTask<bool> TryGrantAsync(RewardGrantRequest rewardRequest, CancellationToken ct = default);
    }
}
