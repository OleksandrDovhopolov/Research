using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Resources.Core;
using Rewards;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionRewardHandler : ICardCollectionRewardHandler
    {
        private readonly IRewardGrantService _rewardGrantService;
        private readonly IRewardDefinitionFactory _rewardDefinitionFactory;
        private readonly CardCollectionRewardsConfigSO _cardCollectionRewardsConfigSo;
        
        public CardCollectionRewardHandler(CardCollectionRewardsConfigSO configSo, IRewardGrantService rewardGrantService, IRewardDefinitionFactory rewardDefinitionFactory)
        {
            _cardCollectionRewardsConfigSo = configSo;
            _rewardGrantService = rewardGrantService;
            _rewardDefinitionFactory = rewardDefinitionFactory;
        }

        public async UniTask<bool> TryHandleGroupCompleted(CardGroupCompletedData groupCompletedData, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            var groupDefinition = _cardCollectionRewardsConfigSo.GroupRewards.FirstOrDefault(group => group.GroupId == groupCompletedData.GroupId);
            if (string.IsNullOrEmpty(groupDefinition.GroupId))
            {
                Debug.LogWarning($"Failed to find GroupRewardDefinition for group with ID {groupCompletedData.GroupId}");
                return false;
            }
            
            var groupRewards = _rewardDefinitionFactory.CreateFromGroupReward(groupDefinition);
            return await ReceiveRewardsAsync(groupRewards, ct);
        }

        public async UniTask<bool> TryHandleCollectionCompleted(CardCollectionCompletedData collectionCompletedData, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            var collectionRewardDefinition = _cardCollectionRewardsConfigSo.FullCollectionReward; 
            var rewardId = collectionRewardDefinition.RewardId; 
            if (rewardId == collectionCompletedData.EventId)
            {
                var collectionRewardModel = _rewardDefinitionFactory.CreateFromCollectionReward(collectionRewardDefinition);
                return await ReceiveRewardsAsync(collectionRewardModel, ct);
            }
            
            Debug.LogError($"Failed to find reward ID {rewardId} for event ID {collectionCompletedData.EventId}");
            return false;
        }

        public async UniTask<bool> TryHandleBuyPointsOffer(string offerId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var exchangeOfferModule = _rewardDefinitionFactory.CreateFromOfferReward(offerId);
            var result = await ReceiveRewardsAsync(exchangeOfferModule, ct);
            
            return result;
        }
        
        private async UniTask<bool> ReceiveRewardsAsync(CollectionRewardDefinition rewardDefinition, CancellationToken ct = default)
        {
            if (rewardDefinition == null || _rewardGrantService == null) 
                return false;
            
            var requests = PrepareRequests(rewardDefinition);
            
            if (requests.Count == 0) return true;
            
            foreach (var request in requests)
            {
                ct.ThrowIfCancellationRequested();
                
                var success = await _rewardGrantService.TryGrantAsync(request, ct);
                if (!success)
                {
                    Debug.LogError($"[Rewards] Failed to grant reward: {request.RewardId}");
                    return false; 
                }
            }

            return true;
        }

        private List<RewardGrantRequest> PrepareRequests(CollectionRewardDefinition rewardDefinition)
        {
            var requests = new List<RewardGrantRequest>();

            var resources = GetResources(rewardDefinition);
            if (resources != null)
            {
                requests.AddRange(resources.Where(r => r is { Amount: > 0 })
                    .Select(r => new RewardGrantRequest(r.Type.ToString(), r.Amount)));
            }

            if (rewardDefinition.CardPack != null)
            {
                requests.AddRange(rewardDefinition.CardPack.Where(p => !string.IsNullOrWhiteSpace(p?.PackId))
                    .Select(p => new RewardGrantRequest(p.PackId, 1)));
            }

            return requests;
        }
        
        private static IReadOnlyCollection<GameResource> GetResources(CollectionRewardDefinition collectionRewardDefinition)
        {
            //TODO add virtual method in CollectionRewardDefinition
            return collectionRewardDefinition switch
            {
                DuplicatePointsChestOffer baseOfferContent => baseOfferContent.Resources,
                FullCollectionReward collectionRewardContent => collectionRewardContent.Resources,
                CardGroupCompletionReward groupCompletedContent => groupCompletedContent.Resources,
                _ => null
            };
        }
    }
}
