using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace core
{
    public class EventCardsService
    {
        private readonly IEventCardsStorage _storage;
        private readonly Dictionary<string, EventCardsSaveData> _cache = new();

        public EventCardsService(IEventCardsStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async UniTask InitializeAsync()
        {
            await _storage.InitializeAsync();
        }

        public async UniTask<EventCardsSaveData> LoadAsync(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));

            if (_cache.TryGetValue(eventId, out var cachedData))
            {
                return cachedData;
            }

            var data = await _storage.LoadAsync(eventId);
            _cache[eventId] = data;
            return data;
        }

        public async UniTask SaveAsync(EventCardsSaveData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            await _storage.SaveAsync(data);
            
            _cache[data.EventId] = data;
        }

        public async UniTask ClearCollectionAsync()
        {
            await _storage.ClearCollectionAsync();
            
            _cache.Clear();
        }
        
        public UniTask UnlockCardAsync(string eventId, string cardId)
        {
            return UnlockCardsAsync(eventId, new[] { cardId });
        }

        public async UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0) return;
            
            await _storage.UnlockCardsAsync(eventId, cardIds);
            
            var updatedData = await _storage.LoadAsync(eventId);
            _cache[eventId] = updatedData;
        }
        
        public bool IsCardUnlocked(string eventId, string cardId)
        {
            var card = _cache[eventId].Cards.Find(c => c.CardId == cardId);
            return card is { IsUnlocked: true };
        }
    }
}