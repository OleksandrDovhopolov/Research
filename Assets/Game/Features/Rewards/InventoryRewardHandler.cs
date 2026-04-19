using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using UnityEngine;

namespace Rewards
{
    public sealed class InventoryRewardHandler : IRewardHandler
    {
        private readonly IInventoryService _inventoryService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;

        public InventoryRewardHandler(IInventoryService inventoryService, IPlayerIdentityProvider playerIdentityProvider)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
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

            var ownerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(ownerId))
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
                ownerId,
                request.RewardId,
                request.Amount,
                request.Category ?? string.Empty);

            try
            {
                await _inventoryService.AddItemAsync(itemDelta, ct);
            }
            catch (NotSupportedException)
            {
                // In server-authoritative mode inventory updates should come from server snapshots.
                Debug.LogWarning($"[Rewards] Client-side inventory add is not supported. RewardId={request.RewardId}.");
            }
        }
    }
}
