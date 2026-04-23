using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public sealed class PlayerStateSnapshotApplier : IPlayerStateSnapshotApplier
    {
        private readonly IReadOnlyList<IPlayerStateSnapshotHandler> _snapshotHandlers;

        public PlayerStateSnapshotApplier(IEnumerable<IPlayerStateSnapshotHandler> snapshotHandlers)
        {
            if (snapshotHandlers == null)
            {
                throw new ArgumentNullException(nameof(snapshotHandlers));
            }

            _snapshotHandlers = snapshotHandlers.Where(handler => handler != null).ToList();
        }

        public async UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (snapshot == null)
            {
                return;
            }

            foreach (var snapshotHandler in _snapshotHandlers)
            {
                ct.ThrowIfCancellationRequested();
                await snapshotHandler.ApplyAsync(snapshot, ct);
            }
        }
    }
}
