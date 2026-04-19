using System;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;

namespace Rewards
{
    public sealed class ResourceRewardHandler : IRewardHandler
    {
        private readonly IResourceOperationsService _resourceOperationsService;

        public ResourceRewardHandler(IResourceOperationsService resourceOperationsService)
        {
            _resourceOperationsService = resourceOperationsService ?? throw new ArgumentNullException(nameof(resourceOperationsService));
        }

        public bool CanHandle(RewardGrantRequest request)
        {
            return request != null && request.Kind == RewardKind.Resource;
        }

        public async UniTask HandleAsync(RewardGrantRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.RewardId))
            {
                throw new ArgumentException("RewardId is empty.", nameof(request));
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Amount must be positive for resource rewards.");
            }

            if (!Enum.TryParse<ResourceType>(request.RewardId, true, out var resourceType))
            {
                throw new InvalidOperationException($"Unsupported resource reward id: {request.RewardId}");
            }

            await _resourceOperationsService.AddAsync(resourceType, request.Amount, ResourceManager.RewardGrantReason, ct);
        }
    }
}
