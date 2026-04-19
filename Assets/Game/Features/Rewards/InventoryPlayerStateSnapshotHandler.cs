using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public sealed class InventoryPlayerStateSnapshotHandler : IPlayerStateSnapshotHandler
    {
        public UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            // TODO: Apply inventory snapshot when player state includes owner/category metadata.
            return UniTask.CompletedTask;
        }
    }
}
