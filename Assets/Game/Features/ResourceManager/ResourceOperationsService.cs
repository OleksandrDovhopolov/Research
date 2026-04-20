using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;

namespace CoreResources
{
    public interface IResourceOperationsService
    {
        UniTask AddAsync(
            ResourceType type,
            int amount,
            string reason = ResourceManager.RewardGrantReason,
            CancellationToken ct = default);

        UniTask<bool> RemoveAsync(
            ResourceType type,
            int amount,
            string reason = ResourceManager.CheatRemoveReason,
            CancellationToken ct = default);
    }

    public sealed class ResourceOperationsService : IResourceOperationsService, IDisposable
    {
        private readonly ResourceManager _resourceManager;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IResourceAdjustApi _resourceAdjustApi;
        private readonly SemaphoreSlim _adjustSemaphore = new(1, 1);
        private bool _isDisposed;

        public ResourceOperationsService(
            ResourceManager resourceManager,
            IPlayerIdentityProvider playerIdentityProvider,
            IResourceAdjustApi resourceAdjustApi)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _resourceAdjustApi = resourceAdjustApi ?? throw new ArgumentNullException(nameof(resourceAdjustApi));
        }

        public async UniTask AddAsync(
            ResourceType type,
            int amount,
            string reason = ResourceManager.RewardGrantReason,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (amount <= 0)
            {
                return;
            }

            await AdjustInternalAsync(type, amount, reason, ct);
        }

        public async UniTask<bool> RemoveAsync(
            ResourceType type,
            int amount,
            string reason = ResourceManager.CheatRemoveReason,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (amount <= 0)
            {
                return false;
            }

            await AdjustInternalAsync(type, -amount, reason, ct);
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _adjustSemaphore.Dispose();
        }

        private async UniTask AdjustInternalAsync(ResourceType type, int delta, string reason, CancellationToken ct)
        {
            ThrowIfDisposed();
            ct.ThrowIfCancellationRequested();
            await _resourceManager.InitializeAsync(ct);

            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            await _adjustSemaphore.WaitAsync(ct);
            try
            {
                var command = new AdjustResourceCommand
                {
                    PlayerId = playerId,
                    ResourceId = type.ToString(),
                    Delta = delta,
                    Reason = string.IsNullOrWhiteSpace(reason) ? ResourceManager.RewardGrantReason : reason
                };

                var response = await _resourceAdjustApi.AdjustAsync(command, ct);
                if (response == null)
                {
                    throw new InvalidOperationException("Resource adjust response is null.");
                }

                if (!response.Success)
                {
                    throw new InvalidOperationException(
                        $"Resource adjust rejected. Code={response.ErrorCode ?? "<none>"}, Message={response.ErrorMessage ?? "<none>"}");
                }

                if (response.Resources == null)
                {
                    throw new InvalidOperationException("Resource adjust response does not contain resources snapshot.");
                }

                await _resourceManager.ApplySnapshotAsync(response.Resources, ct : ct);
            }
            finally
            {
                _adjustSemaphore.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ResourceOperationsService));
            }
        }
    }
}
