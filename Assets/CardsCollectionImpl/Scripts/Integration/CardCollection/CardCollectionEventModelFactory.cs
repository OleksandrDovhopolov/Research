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
        
        private const string EventConfigAddressKey = "eventConfigAddress";
        
        private const string CardPacksAddressKey = "cardPacksAddress";
        private const string FallbackCardPacksAddress = "shared_card_packs_config";
        
        public UniTask<BaseGameEventModel> CreateAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (item == null) throw new ArgumentNullException(nameof(item));

            item.CustomParams ??= new Dictionary<string, string>();
            
            item.CustomParams.TryGetValue(CollectionNameKey, out var collectionName);
            item.CustomParams.TryGetValue(EventConfigAddressKey, out var eventConfigAddress);
            
            
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
                
                CardPacksFileName = cardPacksAddress,
                
                EventConfigAddress = eventConfigAddress,
            };

            return UniTask.FromResult(model);
        }
    }
}
