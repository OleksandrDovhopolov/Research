using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardResponseApplier
    {
        UniTask<bool> TryApplyAsync(GrantRewardResponse response, CancellationToken ct = default);
    }
}
