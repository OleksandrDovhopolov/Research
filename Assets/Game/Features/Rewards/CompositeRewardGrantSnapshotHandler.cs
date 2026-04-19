using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public sealed class CompositeRewardGrantSnapshotHandler : IRewardGrantSnapshotHandler
    {
        private readonly IReadOnlyList<IPlayerStateSnapshotHandler> _handlers;

        public CompositeRewardGrantSnapshotHandler(IEnumerable<IPlayerStateSnapshotHandler> handlers)
        {
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            _handlers = handlers.Where(handler => handler != null).ToList();
        }

        public async UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (snapshot == null)
            {
                return;
            }

            foreach (var playerStateSnapshotHandler in _handlers)
            {
                ct.ThrowIfCancellationRequested();
                await playerStateSnapshotHandler.ApplyAsync(snapshot, ct);
            }
        }
    }
}
