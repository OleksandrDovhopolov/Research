using System;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public sealed class ResourcePlayerStateSnapshotHandler : IPlayerStateSnapshotHandler
    {
        private readonly ResourceManager _resourceManager;

        public ResourcePlayerStateSnapshotHandler(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        }

        public async UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (snapshot?.Resources == null || snapshot.Resources.Count == 0)
            {
                return;
            }

            await _resourceManager.ApplySnapshotAsync(snapshot.Resources, ct);
        }
    }
}
