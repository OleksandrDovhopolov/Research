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
        private const string CollectionIdKey = "eventId";
        private const string RewardsConfigAddressKey = "rewardsConfigAddress";
        private const string DefaultRewardsConfigAddress = "CardCollectionRewardsConfig";

        public UniTask<BaseGameEventModel> CreateAsync(ScheduleItem item, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (item == null) throw new ArgumentNullException(nameof(item));

            item.CustomParams ??= new Dictionary<string, string>();
            item.CustomParams.TryGetValue(CollectionIdKey, out var collectionId);
            item.CustomParams.TryGetValue(RewardsConfigAddressKey, out var rewardsConfigAddress);

            if (string.IsNullOrEmpty(rewardsConfigAddress))
            {
                Debug.LogError($"Failed to resolve RewardsConfigAddressKey. Default used");
                rewardsConfigAddress = DefaultRewardsConfigAddress;
            }
            
            BaseGameEventModel model = new CardCollectionEventModel
            {
                EventId = item.Id,
                EventType = item.EventType,
                StreamId = item.StreamId,
                RewardsConfigAddress = rewardsConfigAddress,
                CollectionId = string.IsNullOrWhiteSpace(collectionId) ? item.Id : collectionId,
            };

            return UniTask.FromResult(model);
        }
    }
}
