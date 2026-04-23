using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IPlayerStateSnapshotApplier
    {
        UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct = default);
    }
}
