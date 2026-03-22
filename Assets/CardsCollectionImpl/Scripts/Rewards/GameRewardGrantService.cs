using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Inventory.API;
using Resources.Core;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class GameRewardGrantService : IRewardGrantService
    {
        private readonly ResourceManager _resourceManager;
        private readonly IInventoryService _inventoryService;
        private readonly string _inventoryOwnerId;

        public GameRewardGrantService(
            ResourceManager resourceManager,
            IInventoryService inventoryService,
            string inventoryOwnerId)
        {
            _resourceManager = resourceManager;
            _inventoryService = inventoryService;
            _inventoryOwnerId = inventoryOwnerId;
        }

        public async UniTask<bool> TryGrantAsync(RewardGrantRequest rewardRequest, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (rewardRequest.Amount <= 0 || string.IsNullOrWhiteSpace(rewardRequest.RewardId))
            {
                Debug.LogWarning($"Failed to add reward. Amount < 0 or RewardId is empty");
                return false;
            }

            if (Enum.TryParse<ResourceType>(rewardRequest.RewardId, true, out var resourceType))
            {
                _resourceManager.Add(resourceType, rewardRequest.Amount);
                return true;
            }

            return await AddToInventoryAsync(rewardRequest, ct);
        }

        private async UniTask<bool> AddToInventoryAsync(RewardGrantRequest rewardRequest, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_inventoryService == null || string.IsNullOrWhiteSpace(_inventoryOwnerId))
            {
                Debug.LogWarning($"Failed to add to Inventory. IInventoryService not initialized or empty _inventoryOwnerId");
                return false;
            }

            var itemDelta = new InventoryItemDelta(
                _inventoryOwnerId,
                rewardRequest.RewardId,
                rewardRequest.Amount,
                CardsConfig.CardPack);

            await _inventoryService.AddItemAsync(itemDelta, ct);
            return true;
        }
    }
}
