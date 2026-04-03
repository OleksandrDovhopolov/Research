using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;

namespace EventOrchestration.Controllers
{
    public sealed class CardCollectionEventModelFactory : IEventModelFactory
    {
        private const string CollectionNameKey = "collectionName";
        private const string EventConfigAddressKey = "eventConfigAddress";
        
        public UniTask<BaseGameEventModel> CreateAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (item == null) throw new ArgumentNullException(nameof(item));

            item.CustomParams ??= new Dictionary<string, string>();
            
            item.CustomParams.TryGetValue(CollectionNameKey, out var collectionName);
            item.CustomParams.TryGetValue(EventConfigAddressKey, out var eventConfigAddress);
            
            BaseGameEventModel model = new CardCollectionEventModel
            {
                EventId = item.Id,
                EventType = item.EventType,
                StreamId = item.StreamId,
                CollectionName = collectionName,
                EventConfigAddress = eventConfigAddress,
            };

            return UniTask.FromResult(model);
        }
    }
}
