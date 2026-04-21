using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardIntentService
    {
        UniTask<CreateRewardIntentResult> CreateAsync(string rewardId, CancellationToken ct = default);
        UniTask<GetRewardIntentStatusResult> GetStatusAsync(string rewardIntentId, CancellationToken ct = default);
    }
}
