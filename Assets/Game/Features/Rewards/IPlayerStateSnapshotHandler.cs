using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public interface IPlayerStateSnapshotHandler
    {
        UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct);
    }
}
