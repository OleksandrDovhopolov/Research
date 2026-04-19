using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IRewardGrantSnapshotHandler
    {
        UniTask ApplyAsync(GrantRewardSnapshotDto snapshot, CancellationToken ct);
    }
}
