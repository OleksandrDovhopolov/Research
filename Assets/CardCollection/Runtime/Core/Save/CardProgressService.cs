using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    public class CardProgressService : IDisposable
    {
        private readonly IEventCardsStorage _storage;
        private readonly Dictionary<string, EventCardsSaveData> _cache = new();
        private readonly Dictionary<string, HashSet<string>> _unlockedCardIdsCache = new();
        private bool _isInitialized;
        private bool _disposed;

        public CardProgressService(IEventCardsStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await _storage.InitializeAsync(ct);
            _isInitialized = true;
        }

        /// <summary>
        /// Ensures that the underlying storage is initialized.
        /// Does NOT create storage or the service – it only runs initialization once.
        /// </summary>
        private async UniTask EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
            {
                return;
            }

            await InitializeAsync(ct);
        }

        public async UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));

            await EnsureInitializedAsync(ct);

            if (_cache.TryGetValue(eventId, out var cachedData))
            {
                return cachedData;
            }

            var data = await _storage.LoadAsync(eventId, ct);
            _cache[eventId] = data;
            return data;
        }

        public async UniTask SaveAsync(EventCardsSaveData data, CancellationToken ct = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            await EnsureInitializedAsync(ct);

            await _storage.SaveAsync(data, ct);
            
            _cache[data.EventId] = data;
            
            // Invalidate unlocked card IDs cache for this event since save might change unlock status
            _unlockedCardIdsCache.Remove(data.EventId);
        }

        public async UniTask AddPointsAsync(string eventId, int pointsToAdd, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));

            if (pointsToAdd <= 0)
                return;

            await EnsureInitializedAsync(ct);

            var currentData = await LoadAsync(eventId, ct);
            currentData.Points += pointsToAdd;

            await SaveAsync(currentData, ct);
        }

        public async UniTask<bool> TrySpendPointsAsync(string eventId, int pointsToSpend, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));

            if (pointsToSpend <= 0)
                return true;

            await EnsureInitializedAsync(ct);

            var currentData = await LoadAsync(eventId, ct);
            if (currentData.Points < pointsToSpend)
            {
                return false;
            }

            currentData.Points -= pointsToSpend;
            await SaveAsync(currentData, ct);

            return true;
        }

        public async UniTask<int> GetPoints(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return 0;

            await EnsureInitializedAsync();
            
            var points = _cache.TryGetValue(eventId, out var data) ? data.Points : 0;
            
            return points;
        }

        public async UniTask ClearCollectionAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            await _storage.ClearCollectionAsync(ct);
            
            _cache.Clear();
            _unlockedCardIdsCache.Clear();
        }
        
        public UniTask UnlockCardAsync(string eventId, string cardId, CancellationToken ct = default)
        {
            return UnlockCardsAsync(eventId, new[] { cardId }, ct);
        }

        public async UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            if (cardIds == null || cardIds.Count == 0) return;

            await EnsureInitializedAsync(ct);

            var currentData = await LoadAsync(eventId, ct);
            var cardsToUnlock = FilterUnlockedCards(currentData, cardIds);
            
            if (cardsToUnlock.Count > 0)
            {
                await _storage.UnlockCardsAsync(eventId, cardsToUnlock, ct);
                
                ApplyUnlockToCache(currentData, cardsToUnlock);
                
                if (_unlockedCardIdsCache.TryGetValue(eventId, out var unlockedIds))
                {
                    unlockedIds.UnionWith(cardsToUnlock);
                }
            }
        }
        
        private static void ApplyUnlockToCache(EventCardsSaveData data, IReadOnlyCollection<string> cardIds)
        {
            if (data?.Cards == null) return;

            foreach (var cardId in cardIds)
            {
                var card = data.Cards.Find(c => c.CardId == cardId);
                if (card != null)
                {
                    card.IsUnlocked = true;
                    card.IsNew = true;
                }
            }
        }
        
        private List<string> FilterUnlockedCards(EventCardsSaveData data, IReadOnlyCollection<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
                return new List<string>();

            if (data?.Cards == null)
                return cardIds.ToList();

            return cardIds
                .Where(cardId =>
                {
                    var card = data.Cards.Find(c => c.CardId == cardId);
                    return card is not { IsUnlocked: true };
                })
                .ToList();
        }
        
        /// <summary>
        /// Gets cards by their IDs from the specified event.
        /// </summary>
        /// <param name="eventId">The event identifier</param>
        /// <param name="cardIds">List of card IDs to retrieve</param>
        /// <param name="ct">Cancellation token for cooperative cancellation</param>
        /// <returns>List of CardProgressData matching the card IDs</returns>
        public async UniTask<List<CardProgressData>> GetCardsByIdsAsync(string eventId, List<string> cardIds, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));
            
            if (cardIds == null || cardIds.Count == 0)
                return new List<CardProgressData>();

            await EnsureInitializedAsync(ct);

            var data = await LoadAsync(eventId, ct);
            
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
        /// <param name="ct">Cancellation token for cooperative cancellation</param>
        public async UniTask ResetNewFlagAsync(string eventId, string cardId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));
            
            if (string.IsNullOrEmpty(cardId))
                throw new ArgumentException("Card ID cannot be null or empty", nameof(cardId));

            await EnsureInitializedAsync(ct);

            var data = await LoadAsync(eventId, ct);
            
            if (data?.Cards == null)
                return;

            var card = data.Cards.Find(c => c.CardId == cardId);
            if (card != null && card.IsNew)
            {
                card.IsNew = false;
                
                Debug.LogWarning($"Debug EventCardsService cardData.CardId {card.CardId} / {card.IsNew}");
                await SaveAsync(data, ct);
            }
        }

        /// <summary>
        /// Gets the IDs of cards that are missing (not unlocked) from the available cards list.
        /// Results are cached per eventId and invalidated when cards are unlocked.
        /// </summary>
        /// <param name="eventId">The event identifier</param>
        /// <param name="allCards">List of all available card definitions</param>
        /// <param name="ct">Cancellation token for cooperative cancellation</param>
        /// <returns>HashSet of card IDs that are missing (not unlocked)</returns>
        internal async UniTask<HashSet<string>> GetMissingCardIdsAsync(string eventId, List<CardDefinition> allCards, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));

            if (allCards == null || allCards.Count == 0)
                return new HashSet<string>();

            await EnsureInitializedAsync(ct);

            // Get or compute unlocked card IDs cache
            if (!_unlockedCardIdsCache.TryGetValue(eventId, out var unlockedCardIds))
            {
                var progressData = await LoadAsync(eventId, ct);
                unlockedCardIds = new HashSet<string>(
                    progressData.Cards.Where(c => c.IsUnlocked).Select(c => c.CardId));
                _unlockedCardIdsCache[eventId] = unlockedCardIds;
            }

            // Return missing card IDs (cards not in unlocked set)
            return new HashSet<string>(
                allCards.Where(c => !unlockedCardIds.Contains(c.Id)).Select(c => c.Id));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cache.Clear();
            _unlockedCardIdsCache.Clear();
            _storage.Dispose();
        }
    }
}
