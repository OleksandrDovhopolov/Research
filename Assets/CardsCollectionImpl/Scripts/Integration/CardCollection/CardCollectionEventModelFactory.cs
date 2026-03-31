using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using UnityEngine;

namespace EventOrchestration.Controllers
{
    public sealed class CardCollectionEventModelFactory : IEventModelFactory
    {
        private const string CollectionNameKey = "collectionName";
        
        private const string RewardsConfigAddressKey = "rewardsConfigAddress";
        private const string FallbackRewardsConfigAddress = "CardCollectionRewardsConfig";
        
        private const string CardsCollectionAddressKey = "cardsCollectionAddress";
        private const string FallbackCollectionAddress = "fallback_card_collection";
        
        private const string CardGroupsAddressKey = "cardGroupsAddress";
        private const string FallbackCardGroupsAddress = "fallback_card_groups";
        
        private const string CardPacksAddressKey = "cardPacksAddress";
        private const string FallbackCardPacksAddress = "shared_card_packs_config";
        
        public UniTask<BaseGameEventModel> CreateAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (item == null) throw new ArgumentNullException(nameof(item));

            item.CustomParams ??= new Dictionary<string, string>();
            
            item.CustomParams.TryGetValue(CollectionNameKey, out var collectionName);
            
            item.CustomParams.TryGetValue(RewardsConfigAddressKey, out var rewardsConfigAddress);
            if (string.IsNullOrEmpty(rewardsConfigAddress))
            {
                Debug.LogError($"Failed to resolve RewardsConfigAddressKey. Default used");
                rewardsConfigAddress = FallbackRewardsConfigAddress;
            }
            
            item.CustomParams.TryGetValue(CardsCollectionAddressKey, out var cardsCollectionAddress);
            if (string.IsNullOrEmpty(cardsCollectionAddress))
            {
                Debug.LogError($"Failed to resolve CardsCollectionConfigAddressKey. Default used");
                cardsCollectionAddress = FallbackCollectionAddress;
            }

            item.CustomParams.TryGetValue(CardGroupsAddressKey, out var cardGroupsAddress);
            if (string.IsNullOrEmpty(cardGroupsAddress))
            {
                Debug.LogError($"Failed to resolve CardGroupsConfigAddressKey. Default used");
                cardGroupsAddress = FallbackCardGroupsAddress;
            }
            
            item.CustomParams.TryGetValue(CardPacksAddressKey, out var cardPacksAddress);
            if (string.IsNullOrEmpty(cardPacksAddress))
            {
                Debug.LogError($"Failed to resolve CardPacksConfigAddressKey. Default used");
                cardPacksAddress = FallbackCardPacksAddress;
            }
            
            BaseGameEventModel model = new CardCollectionEventModel
            {
                EventId = item.Id,
                EventType = item.EventType,
                StreamId = item.StreamId,
                CollectionName = collectionName,
                RewardsConfigAddress = rewardsConfigAddress,
                CardCollectionFileName = cardsCollectionAddress,
                GroupsFileName = cardGroupsAddress,
                CardPacksFileName = cardPacksAddress,
            };

            return UniTask.FromResult(model);
        }
    }
}
