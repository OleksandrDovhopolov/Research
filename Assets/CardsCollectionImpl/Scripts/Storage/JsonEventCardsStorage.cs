using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure.SaveSystem;

namespace CardCollectionImpl
{
    public class JsonEventCardsStorage : IEventCardsStorage
    {
        private readonly SaveService _saveService;
        private bool _disposed;

        public JsonEventCardsStorage(SaveService saveService)
        {
            _saveService = saveService;
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await _saveService.LoadAllAsync(ct);
        }

        public async UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default)
        {
            ValidateEventId(eventId);

            var saveData = await _saveService.GetModuleAsync(data =>
            {
                var found = data.CardCollections.Find(x => x.EventId == eventId);
                return found == null ? null : CloneCardCollection(found);
            }, ct);
            var data = ToEventCardsSaveData(saveData);

            if (data == null)
            {
                return new EventCardsSaveData { EventId = eventId, Version = 1 };
            }

            if (string.IsNullOrEmpty(data.EventId))
            {
                data.EventId = eventId;
            }

            return data;
        }

        public async UniTask SaveAsync(EventCardsSaveData data, CancellationToken ct = default)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ValidateEventId(data.EventId);
            var moduleData = ToModuleData(data);
            await _saveService.UpdateModuleAsync(root => root.CardCollections, cardCollections =>
            {
                var index = cardCollections.FindIndex(x => x.EventId == moduleData.EventId);
                if (index < 0)
                {
                    cardCollections.Add(moduleData);
                    return;
                }

                cardCollections[index] = moduleData;
            }, ct);
        }

        public async UniTask UnlockCardsAsync(EventCardsSaveData data, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            if (cardIds == null || cardIds.Count == 0)
                return;

            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            ValidateEventId(data.EventId);

            var cardDict = data.Cards.ToDictionary(c => c.CardId, c => c);

            foreach (var cardId in cardIds)
            {
                if (string.IsNullOrEmpty(cardId))
                {
                    continue;
                }

                if (cardDict.TryGetValue(cardId, out var existingCard))
                {
                    existingCard.IsUnlocked = true;
                    existingCard.IsNew = true;
                }
            }

            await SaveAsync(data, ct);
        }

        private static void ValidateEventId(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        private static CardCollectionModuleSaveData ToModuleData(EventCardsSaveData data)
        {
            return new CardCollectionModuleSaveData
            {
                EventId = data.EventId,
                Version = data.Version <= 0 ? 1 : data.Version,
                Points = data.Points,
                Cards = data.Cards?.Select(x => new CardProgressSaveData
                {
                    CardId = x.CardId,
                    IsUnlocked = x.IsUnlocked,
                    IsNew = x.IsNew,
                }).ToList() ?? new List<CardProgressSaveData>(),
            };
        }

        private static EventCardsSaveData ToEventCardsSaveData(CardCollectionModuleSaveData data)
        {
            if (data == null)
            {
                return null;
            }

            return new EventCardsSaveData
            {
                EventId = data.EventId,
                Version = data.Version,
                Points = data.Points,
                Cards = data.Cards?.Select(x => new CardProgressData
                {
                    CardId = x.CardId,
                    IsUnlocked = x.IsUnlocked,
                    IsNew = x.IsNew,
                }).ToList() ?? new List<CardProgressData>(),
            };
        }

        private static CardCollectionModuleSaveData CloneCardCollection(CardCollectionModuleSaveData source)
        {
            return new CardCollectionModuleSaveData
            {
                EventId = source.EventId,
                Version = source.Version,
                Points = source.Points,
                Cards = source.Cards?.Select(x => new CardProgressSaveData
                {
                    CardId = x.CardId,
                    IsUnlocked = x.IsUnlocked,
                    IsNew = x.IsNew,
                }).ToList() ?? new List<CardProgressSaveData>(),
            };
        }
    }
}
