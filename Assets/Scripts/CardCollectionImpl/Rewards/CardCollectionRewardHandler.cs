using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public sealed class CardCollectionRewardHandler
    {
        private const string DefaultRewardsConfigAddress = "CardCollectionRewardsConfig";

        private readonly ResourceManager _resourceManager;
        private Dictionary<string, GameResource> _groupRewardByGroupId = new(StringComparer.Ordinal);
        private bool _isInitialized;

        public CardCollectionRewardHandler(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_resourceManager == null)
            {
                Debug.LogError("[CardCollectionRewardHandler] ResourceManager is null. Rewards cannot be applied.");
                return;
            }
            
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
                return TryApplyReward(reward);
            }
            
            return false;
        }

        private bool TryApplyReward(GameResource reward)
        {
            if (_resourceManager == null || reward == null || reward.Amount <= 0)
            {
                return false;
            }

            _resourceManager.Add(reward.Type, reward.Amount);
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

            foreach (var kvp in result)
            {
                Debug.LogWarning($"Debug Test groupCompletedData {kvp.Key}");
            }
            
            return result;
        }
    }
}
