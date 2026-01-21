using System;
using System.Collections.Generic;
using System.Linq;
using core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
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
            
            var currentData = await LoadAsync(eventId);
            var cardsToUnlock = currentData.FilterUnlockedCards(cardIds);
            
            if (cardsToUnlock.Count > 0)
            {
                await _storage.UnlockCardsAsync(eventId, cardsToUnlock);
                
                var updatedData = await _storage.LoadAsync(eventId);
                _cache[eventId] = updatedData;
            }
        }
        
        public bool IsCardUnlocked(string eventId, string cardId)
        {
            var card = _cache[eventId].Cards.Find(c => c.CardId == cardId);
            return card is { IsUnlocked: true };
        }
        
        /// <summary>
        /// Gets cards by their IDs from the specified event.
        /// </summary>
        /// <param name="eventId">The event identifier</param>
        /// <param name="cardIds">List of card IDs to retrieve</param>
        /// <returns>List of CardProgressData matching the card IDs</returns>
        public async UniTask<List<CardProgressData>> GetCardsByIdsAsync(string eventId, List<string> cardIds)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));
            
            if (cardIds == null || cardIds.Count == 0)
                return new List<CardProgressData>();

            var data = await LoadAsync(eventId);
            
            if (data?.Cards == null)
                return new List<CardProgressData>();

            return data.Cards
                .Where(card => cardIds.Contains(card.CardId))
                .ToList();
        }
        
        /// <summary>
        /// Resets the IsNew flag for the specified card.
        /// </summary>
        /// <param name="eventId">The event identifier</param>
        /// <param name="cardId">The card identifier to reset</param>
        public async UniTask ResetNewFlagAsync(string eventId, string cardId)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));
            
            if (string.IsNullOrEmpty(cardId))
                throw new ArgumentException("Card ID cannot be null or empty", nameof(cardId));

            var data = await LoadAsync(eventId);
            
            if (data?.Cards == null)
                return;

            var card = data.Cards.Find(c => c.CardId == cardId);
            if (card != null && card.IsNew)
            {
                card.IsNew = false;
                
                Debug.LogWarning($"Debug EventCardsService cardData.CardId {card.CardId} / {card.IsNew}");
                await SaveAsync(data);
            }
        }
    }
}