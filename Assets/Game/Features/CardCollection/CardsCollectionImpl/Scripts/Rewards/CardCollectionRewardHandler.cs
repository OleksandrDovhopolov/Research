using System;
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
        private readonly CardCollectionStaticData _collectionStaticData;
        
        public CardCollectionRewardHandler(CardCollectionStaticData collectionStaticData, IRewardSpecProvider rewardSpecProvider, IRewardGrantService rewardGrantService)
        {
            _collectionStaticData = collectionStaticData;
            _rewardGrantService = rewardGrantService;
            _rewardSpecProvider =  rewardSpecProvider;
        }

        public async UniTask<bool> TryHandleGroupCompleted(CardGroupCompletedData groupCompletedData, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            var groupRewardConfig = _collectionStaticData.Rewards.FirstOrDefault(group => group.rewardId == groupCompletedData.GroupType);

            if (groupRewardConfig == null || string.IsNullOrEmpty(groupRewardConfig.rewardId))
            {
                Debug.LogWarning($"Failed to find GroupRewardDefinition for group with ID {groupCompletedData.GroupType}");
                return false;
            }

            return await ReceiveRewardsAsync(groupRewardConfig.rewardItemId, ct);
        }

        public async UniTask<bool> TryHandleCollectionCompleted(CardCollectionCompletedData collectionCompletedData, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            var groupRewardConfig = _collectionStaticData.Rewards.FirstOrDefault(group => group.rewardId == collectionCompletedData.EventId);
            if (groupRewardConfig == null || string.IsNullOrEmpty(groupRewardConfig.rewardId))
            {
                Debug.LogWarning($"Failed to find GroupRewardDefinition for group with ID {collectionCompletedData.EventId}");
                return false;
            }
            var rewardId = groupRewardConfig.rewardItemId; 

            var success = await ReceiveRewardsAsync(rewardId, ct);
            if (success) return true;
            
            Debug.LogError($"Failed to find reward ID {rewardId} for event ID {collectionCompletedData.EventId}");
            return false;
        }

        public async UniTask<bool> TryHandleBuyPointsOffer(string offerId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var success = await ReceiveRewardsAsync(offerId, ct);
            if (success) return true;
            
            Debug.LogError($"Failed to find offer reward ID {offerId}");
            return false;
        }
        
        private async UniTask<bool> ReceiveRewardsAsync(string rewardId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(rewardId) || _rewardGrantService == null)
            {
                return false;
            }

            return await _rewardGrantService.TryGrantAsync(rewardId, ct);
        }
        
        public RewardViewData CreateRewardViewData(string groupType)
        {
            if (string.IsNullOrEmpty(groupType) || _collectionStaticData.Rewards == null)
                return RewardViewData.Empty;

            foreach (var groupReward in _collectionStaticData.Rewards)
            {
                if (!string.Equals(groupReward.rewardId, groupType, StringComparison.Ordinal))
                    continue;

                if (!_rewardSpecProvider.TryGet(groupReward.rewardItemId, out var spec))
                    return RewardViewData.Empty;

                return new RewardViewData(groupReward.rewardItemId, spec.Icon, spec.TotalAmountForUi);
            }

            return RewardViewData.Empty;
        }
    }
}
