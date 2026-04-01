using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Rewards;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionRewardHandler : ICardCollectionRewardHandler
    {
        private readonly IRewardSpecProvider _rewardSpecProvider;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly CardCollectionRewardsConfigSO _cardCollectionRewardsConfigSo;
        
        public CardCollectionRewardHandler(CardCollectionRewardsConfigSO configSo, IRewardSpecProvider rewardSpecProvider, IRewardGrantService rewardGrantService)
        {
            _cardCollectionRewardsConfigSo = configSo;
            _rewardGrantService = rewardGrantService;
            _rewardSpecProvider =  rewardSpecProvider;
        }

        public async UniTask<bool> TryHandleGroupCompleted(CardGroupCompletedData groupCompletedData, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            var groupRewardConfig = _cardCollectionRewardsConfigSo.GroupRewards.FirstOrDefault(group => group.GroupId == groupCompletedData.GroupType);
            
            if (string.IsNullOrEmpty(groupRewardConfig.GroupId))
            {
                Debug.LogWarning($"Failed to find GroupRewardDefinition for group with ID {groupCompletedData.GroupType}");
                return false;
            }
            
            if (!_rewardSpecProvider.TryGet(groupRewardConfig.RewardId, out var spec))
            {
                throw new Exception($"Unknown reward id: {groupRewardConfig.RewardId}");
            }
            
            return await ReceiveRewardsAsync(spec, ct);
        }

        public async UniTask<bool> TryHandleCollectionCompleted(CardCollectionCompletedData collectionCompletedData, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            var collectionRewardDefinition = _cardCollectionRewardsConfigSo.FullCollectionReward; 
            var rewardId = collectionRewardDefinition.RewardId; 
            
            if (!_rewardSpecProvider.TryGet(rewardId, out var spec))
            {
                throw new Exception($"Unknown reward id: {rewardId}");
            }
            
            var success = await ReceiveRewardsAsync(spec, ct);
            if (success) return true;
            
            Debug.LogError($"Failed to find reward ID {rewardId} for event ID {collectionCompletedData.EventId}");
            return false;
        }

        public async UniTask<bool> TryHandleBuyPointsOffer(string offerId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            if (!_rewardSpecProvider.TryGet(offerId, out var spec))
            {
                throw new Exception($"Unknown reward id: {offerId}");
            }
            
            var success = await ReceiveRewardsAsync(spec, ct);
            if (success) return true;
            
            Debug.LogError($"Failed to find offer reward ID {offerId}");
            return false;
        }
        
        private async UniTask<bool> ReceiveRewardsAsync(RewardSpec rewardSpec, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (rewardSpec == null || _rewardGrantService == null)
            {
                return false;
            }

            var resources = rewardSpec.Resources;
            if (resources == null || resources.Count == 0)
            {
                return false;
            }

            var requests = new List<RewardGrantRequest>(resources.Count);
            foreach (var resource in resources)
            {
                ct.ThrowIfCancellationRequested();
                if (resource == null || string.IsNullOrWhiteSpace(resource.ResourceId) || resource.Amount <= 0)
                {
                    continue;
                }

                requests.Add(new RewardGrantRequest(resource.ResourceId, resource.Amount, resource.Category));
            }

            if (requests.Count == 0)
            {
                Debug.LogWarning($"[Rewards] RewardSpec {rewardSpec.RewardId} has no valid resources");
                return false;
            }

            return await _rewardGrantService.TryGrantAsync(requests, ct);
        }
    }
}
