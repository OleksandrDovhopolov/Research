using System;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;
using Inventory.API;
using UIShared;
using UnityEngine;

namespace Rewards
{
    public sealed class GameRewardGrantService : IRewardGrantService
    {
        private readonly ResourceManager _resourceManager;
        private readonly IInventoryService _inventoryService;
        private readonly AnimateCurrency _animateCurrency;
        private readonly string _inventoryOwnerId;

        public GameRewardGrantService(
            AnimateCurrency animateCurrency,
            ResourceManager resourceManager,
            IInventoryService inventoryService,
            string inventoryOwnerId)
        {
            _animateCurrency =  animateCurrency;
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
                
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, resourceType,  rewardRequest.Amount);
                _animateCurrency.Animate(animationArgs, OnAnimationCompleted);
                return true;
            }

            return await AddToInventoryAsync(rewardRequest, ct);
            
            void OnAnimationCompleted()
            {
                _resourceManager.NotifyAmountChanged(resourceType);
            }
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
                rewardRequest.Category); // TODO need this Category in DTO ?

            await _inventoryService.AddItemAsync(itemDelta, ct);
            return true;
        }
    }
}
