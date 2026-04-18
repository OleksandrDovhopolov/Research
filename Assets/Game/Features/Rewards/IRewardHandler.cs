using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardHandler
    {
        bool CanHandle(RewardGrantRequest request);

        UniTask HandleAsync(RewardGrantRequest request, CancellationToken ct);
    }
}
