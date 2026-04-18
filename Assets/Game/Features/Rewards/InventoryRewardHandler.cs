using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Inventory.API;

namespace Rewards
{
    public sealed class InventoryRewardHandler : IRewardHandler
    {
        private readonly IInventoryService _inventoryService;
        private readonly string _inventoryOwnerId;

        public InventoryRewardHandler(IInventoryService inventoryService, string inventoryOwnerId)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _inventoryOwnerId = inventoryOwnerId ?? throw new ArgumentNullException(nameof(inventoryOwnerId));
        }

        public bool CanHandle(RewardGrantRequest request)
        {
            return request != null && request.Kind == RewardKind.InventoryItem;
        }

        public async UniTask HandleAsync(RewardGrantRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(_inventoryOwnerId))
            {
                throw new InvalidOperationException("Inventory owner id is empty.");
            }

            if (string.IsNullOrWhiteSpace(request.RewardId))
            {
                throw new ArgumentException("RewardId is empty.", nameof(request));
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Amount must be positive for inventory rewards.");
            }

            var itemDelta = new InventoryItemDelta(
                _inventoryOwnerId,
                request.RewardId,
                request.Amount,
                request.Category ?? string.Empty);

            await _inventoryService.AddItemAsync(itemDelta, ct);
        }
    }
}
