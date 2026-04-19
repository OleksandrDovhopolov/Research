using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public sealed class NoOpRewardGrantSnapshotHandler : IRewardGrantSnapshotHandler
    {
        public UniTask ApplyAsync(GrantRewardSnapshotDto snapshot, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }
    }
}
