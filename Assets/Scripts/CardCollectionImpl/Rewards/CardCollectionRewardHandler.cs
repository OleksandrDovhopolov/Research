using System;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public sealed class CardCollectionRewardHandler
    {
        private const string DefaultRewardsConfigAddress = "CardCollectionRewardsConfig";

        private readonly IOfferRewardsReceiver _offerRewardsReceiver;
        private readonly IRewardDefinitionFactory _rewardDefinitionFactory;
        
        private bool _isInitialized;
        private CardCollectionRewardsConfigSO _cardCollectionRewardsConfigSo;
        
        public CardCollectionRewardHandler(IOfferRewardsReceiver offerRewardsReceiver, IRewardDefinitionFactory rewardDefinitionFactory)
        {
            _offerRewardsReceiver = offerRewardsReceiver;
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

        public bool TryHandleGroupCompleted(CardGroupCompletedData groupCompletedData)
        {
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
            //TODO await this 
            _offerRewardsReceiver.ReceiveRewardsAsync(groupRewards).Forget();
            return true;
        }

        public bool TryHandleCollectionCompleted(CardCollectionCompletedData collectionCompletedData)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[CardCollectionRewardHandler] Rewards config is not loaded. Call InitializeAsync before handling rewards.");
                return false;
            }

            var collectionRewardDefinition = _cardCollectionRewardsConfigSo.FullCollectionReward; 
            if (collectionRewardDefinition.RewardId == collectionCompletedData.EventId)
            {
                var collectionRewardModel = _rewardDefinitionFactory.CreateFromCollectionReward(collectionRewardDefinition);
                //TODO await this 
                _offerRewardsReceiver.ReceiveRewardsAsync(collectionRewardModel).Forget();
                return true;
            }
            
            Debug.LogWarning($"Failed to find CollectionRewardDefinition for group with ID groupCompletedData.GroupId");
            return false;
        }
    }
}
