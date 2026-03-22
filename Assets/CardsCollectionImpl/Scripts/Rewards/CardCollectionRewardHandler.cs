using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Resources.Core;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionRewardHandler : ICardCollectionRewardHandler, IDisposable
    {
        private const string DefaultRewardsConfigAddress = "CardCollectionRewardsConfig";

        private readonly IRewardGrantService _rewardGrantService;
        private readonly IRewardDefinitionFactory _rewardDefinitionFactory;
        
        private bool _isInitialized;
        private CardCollectionRewardsConfigSO _cardCollectionRewardsConfigSo;
        
        public CardCollectionRewardHandler(IRewardGrantService rewardGrantService, IRewardDefinitionFactory rewardDefinitionFactory)
        {
            _rewardGrantService = rewardGrantService;
            _rewardDefinitionFactory = rewardDefinitionFactory;
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            try
            {
                _cardCollectionRewardsConfigSo = await AddressablesWrapper.LoadFromTask<CardCollectionRewardsConfigSO>(DefaultRewardsConfigAddress)
                    .AsUniTask()
                    .AttachExternalCancellation(ct);

                if (_cardCollectionRewardsConfigSo == null)
                {
                    Debug.LogError(
                        $"[CardCollectionRewardHandler] Failed to load rewards config from address '{DefaultRewardsConfigAddress}': loaded asset is null.");
                    return;
                }

                _isInitialized = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[CardCollectionRewardHandler] Failed to load rewards config from address '{DefaultRewardsConfigAddress}'. Exception: {e}");
            }
        }

        public async UniTask<bool> TryHandleGroupCompleted(CardGroupCompletedData groupCompletedData, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!_isInitialized)
            {
                Debug.LogError("[CardCollectionRewardHandler] Rewards config is not loaded. Call InitializeAsync before handling rewards.");
                return false;
            }

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
            
            if (!_isInitialized)
            {
                Debug.LogError("[CardCollectionRewardHandler] Rewards config is not loaded. Call InitializeAsync before handling rewards.");
                return false;
            }

            var collectionRewardDefinition = _cardCollectionRewardsConfigSo.FullCollectionReward; 
            if (collectionRewardDefinition.RewardId == collectionCompletedData.EventId)
            {
                var collectionRewardModel = _rewardDefinitionFactory.CreateFromCollectionReward(collectionRewardDefinition);
                return await ReceiveRewardsAsync(collectionRewardModel, ct);
            }
            
            Debug.LogError($"Failed to find CollectionRewardDefinition for group with ID groupCompletedData.GroupId");
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

            var requests = new List<RewardGrantRequest>();

            var resources = GetResources(rewardDefinition);
            if (resources != null)
            {
                requests.AddRange(resources
                    .Where(r => r is { Amount: > 0 })
                    .Select(r => new RewardGrantRequest(r.Type.ToString(), r.Amount)));
            }

            if (rewardDefinition.CardPack != null)
            {
                requests.AddRange(rewardDefinition.CardPack
                    .Where(p => !string.IsNullOrWhiteSpace(p?.PackId))
                    .Select(p => new RewardGrantRequest(p.PackId, 1)));
            }

            if (requests.Count == 0) return true;

            foreach (var request in requests)
            {
                ct.ThrowIfCancellationRequested();

                //TODO 1. if returned false in the middle all other rewards would be skipped 
                //TODO 2. try to make reward only by id  and process it on IRewardGrantService side. 
                //TODO 3 ExchangePacksConfig should be outside Impl and coupled with IRewardGrantService  or be near in reward logic? 
                
                var success = await _rewardGrantService.TryGrantAsync(request, ct);
                if (!success)
                {
                    Debug.LogError($"[Rewards] Failed to grant reward: {request.RewardId}");
                    return false; 
                }
            }

            return true;
        }
        
        private static IReadOnlyCollection<GameResource> GetResources(CollectionRewardDefinition collectionRewardDefinition)
        {
            return collectionRewardDefinition switch
            {
                DuplicatePointsChestOffer baseOfferContent => baseOfferContent.Resources,
                FullCollectionReward collectionRewardContent => collectionRewardContent.Resources,
                CardGroupCompletionReward groupCompletedContent => groupCompletedContent.Resources,
                _ => null
            };
        }
        
        public void Dispose()
        {
            if (_cardCollectionRewardsConfigSo != null)
            {
                AddressablesWrapper.Release(_cardCollectionRewardsConfigSo);
                _cardCollectionRewardsConfigSo = null;
            }

            _isInitialized = false;
        }
    }
}
