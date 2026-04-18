using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public class CardProgressService : IDisposable
    {
        private readonly IEventCardsStorage _storage;
        private readonly ICardDefinitionProvider _cardDefinitionProvider;
        private readonly ICardPointsCalculator _pointsCalculator;
        private readonly Dictionary<string, EventCardsSaveData> _cache = new();
        private bool _isInitialized;
        private bool _disposed;

        public CardProgressService(
            IEventCardsStorage storage,
            ICardDefinitionProvider cardDefinitionProvider,
            ICardPointsCalculator pointsCalculator)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _cardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
            _pointsCalculator = pointsCalculator ?? throw new ArgumentNullException(nameof(pointsCalculator));
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await _storage.InitializeAsync(ct);
            _isInitialized = true;
        }

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

        public async UniTask<int> AddDuplicatePointsAsync(
            string eventId,
            IReadOnlyList<string> openedCardIds,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));

            if (openedCardIds == null || openedCardIds.Count == 0)
                return 0;

            await EnsureInitializedAsync(ct);

            var currentData = await LoadAsync(eventId, ct);
            if (currentData?.Cards == null || currentData.Cards.Count == 0)
                return 0;

            var cardDefinitionsById = _cardDefinitionProvider.GetCardDefinitionsById();
            if (cardDefinitionsById == null || cardDefinitionsById.Count == 0)
                return 0;

            var unlockedCardIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CardProgressData cardProgress in currentData.Cards)
            {
                if (cardProgress is { IsUnlocked: true } && !string.IsNullOrEmpty(cardProgress.CardId))
                {
                    unlockedCardIds.Add(cardProgress.CardId);
                }
            }

            if (unlockedCardIds.Count == 0)
                return 0;

            var awardedPoints = 0;
            foreach (var cardId in openedCardIds)
            {
                if (string.IsNullOrEmpty(cardId) || !unlockedCardIds.Contains(cardId))
                {
                    continue;
                }

                if (!cardDefinitionsById.TryGetValue(cardId, out var cardDefinition))
                {
                    continue;
                }

                var pointsForCard = _pointsCalculator.GetPoints(cardDefinition.Stars, cardDefinition.PremiumCard);
                if (pointsForCard <= 0)
                {
                    continue;
                }

                awardedPoints += pointsForCard;
            }

            if (awardedPoints <= 0)
                return 0;

            currentData.Points += awardedPoints;
            await SaveAsync(currentData, ct);

            return awardedPoints;
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

        public async UniTask<int> GetPoints(string eventId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                return 0;

            await EnsureInitializedAsync(ct);
            
            var points = _cache.TryGetValue(eventId, out var data) ? data.Points : 0;
            
            return points;
        }

        public async UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            if (cardIds == null || cardIds.Count == 0) return;

            await EnsureInitializedAsync(ct);

            var currentData = await LoadAsync(eventId, ct);
            var cardsToUnlock = FilterUnlockedCards(currentData, cardIds);
            
            if (cardsToUnlock.Count > 0)
            {
                await _storage.UnlockCardsAsync(currentData, cardsToUnlock, ct);
                
                ApplyUnlockToCache(currentData, cardsToUnlock);
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

        public async UniTask ResetNewFlagsAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));

            if (cardIds == null || cardIds.Count == 0)
                return;

            await EnsureInitializedAsync(ct);

            var data = await LoadAsync(eventId, ct);

            if (data?.Cards == null)
                return;

            var idsToReset = new HashSet<string>(
                cardIds.Where(id => !string.IsNullOrEmpty(id)),
                StringComparer.Ordinal);

            if (idsToReset.Count == 0)
                return;

            var hasChanges = false;
            foreach (var card in data.Cards)
            {
                if (card is { IsNew: true } && idsToReset.Contains(card.CardId))
                {
                    card.IsNew = false;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await SaveAsync(data, ct);
            }
        }

        public async UniTask PurgeEventDataAsync(string eventId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(eventId))
                throw new ArgumentException("Event ID cannot be null or empty", nameof(eventId));

            await EnsureInitializedAsync(ct);
            await _storage.DeleteAsync(eventId, ct);
            _cache.Remove(eventId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cache.Clear();
            _storage.Dispose();
        }
    }
}
