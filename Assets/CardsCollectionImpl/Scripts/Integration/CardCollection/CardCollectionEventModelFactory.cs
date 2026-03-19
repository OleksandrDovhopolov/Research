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
        private const string CollectionIdKey = "eventId";

        public UniTask<BaseGameEventModel> CreateAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (item == null) throw new ArgumentNullException(nameof(item));

            item.CustomParams ??= new Dictionary<string, string>();
            item.CustomParams.TryGetValue(CollectionIdKey, out var collectionId);

            BaseGameEventModel model = new CardCollectionEventModel
            {
                EventId = item.Id,
                EventType = item.EventType,
                StreamId = item.StreamId,
                CollectionId = string.IsNullOrWhiteSpace(collectionId) ? item.Id : collectionId,
            };

            return UniTask.FromResult(model);
        }
    }
}
