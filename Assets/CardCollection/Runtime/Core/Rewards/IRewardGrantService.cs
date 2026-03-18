using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IRewardGrantService
    {
        UniTask<bool> TryGrantAsync(RewardGrantRequest rewardRequest, CancellationToken ct = default);
    }
}
