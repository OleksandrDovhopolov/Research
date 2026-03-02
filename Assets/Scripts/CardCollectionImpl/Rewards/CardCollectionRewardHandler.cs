using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public sealed class CardCollectionRewardHandler
    {
        private const string DefaultRewardsConfigAddress = "CardCollectionRewardsConfig";

        private readonly IOfferRewardsReceiver _offerRewardsReceiver;
        private Dictionary<string, GameResource> _groupRewardByGroupId = new(StringComparer.Ordinal);
            
        private bool _isInitialized;

        public CardCollectionRewardHandler(IOfferRewardsReceiver  offerRewardsReceiver)
        {
            _offerRewardsReceiver = offerRewardsReceiver;
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            try
            {
                var config = await AddressablesWrapper.LoadFromTask<CardCollectionRewardsConfigSO>(DefaultRewardsConfigAddress)
                    .AsUniTask()
                    .AttachExternalCancellation(ct);

                if (config == null)
                {
                    Debug.LogError(
                        $"[CardCollectionRewardHandler] Failed to load rewards config from address '{DefaultRewardsConfigAddress}': loaded asset is null.");
                    return;
                }

                _groupRewardByGroupId = BuildGroupRewards(config.GroupRewards);
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

            if (string.IsNullOrEmpty(groupCompletedData.GroupId))
            {
                Debug.LogWarning($"No reward configured for completed group '{groupCompletedData.GroupId}'.");
                return false;
            }

            if (_groupRewardByGroupId.TryGetValue(groupCompletedData.GroupId, out var reward))
            {
                var groupCompletedContent = new CardGroupCompletedContent
                {
                    Source = RewardSource.GroupCompleted,
                };
                groupCompletedContent.Resources.Add(reward);

                //TODO await this + handle result + add token
                _offerRewardsReceiver.ReceiveRewardsAsync(groupCompletedContent).Forget();
                return true;
            }
            
            return false;
        }

        public bool TryHandleCollectionCompleted(OfferContent collectionRewardContent)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[CardCollectionRewardHandler] Rewards config is not loaded. Call InitializeAsync before handling rewards.");
                return false;
            }

            //TODO await this 
            _offerRewardsReceiver.ReceiveRewardsAsync(collectionRewardContent).Forget();
            return true;
        }

        private static Dictionary<string, GameResource> BuildGroupRewards(
            IReadOnlyCollection<GroupRewardDefinition> groupRewardDefinitions)
        {
            var result = new Dictionary<string, GameResource>(StringComparer.Ordinal);
            if (groupRewardDefinitions == null)
            {
                return result;
            }

            foreach (var rewardDefinition in groupRewardDefinitions)
            {
                if (string.IsNullOrEmpty(rewardDefinition.GroupId) || rewardDefinition.Amount <= 0)
                {
                    continue;
                }

                if (!Enum.TryParse(rewardDefinition.RewardId, true, out ResourceType resourceType))
                {
                    continue;
                }

                result[rewardDefinition.GroupId] = new GameResource(resourceType, rewardDefinition.Amount);
            }
            
            return result;
        }
    }
}
