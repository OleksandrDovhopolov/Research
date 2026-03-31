using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    public sealed class CardCollectionModule : 
        ICardCollectionModule, 
        ICardCollectionReader, 
        ICardCollectionUpdater, 
        ICardCollectionPointsAccount, 
        ICardGroupCompletionNotifier,
        ICardCollectionCompletionNotifier,
        IDisposable
    {
        private readonly CardCollectionContext _context;
        private readonly CardSelectionContext _selectionContext;
        
        private GroupCompletionTracker _groupCompletionTracker;
        private bool _isCollectionCompleted;
        
        public event Action<CardGroupsCompletedData> OnGroupCompleted;
        public event Action<CardCollectionCompletedData> OnCollectionCompleted;
        
        public CardCollectionModule(CardCollectionModuleConfig  config)
        {
            _context = new CardCollectionContext(config);
            _selectionContext = new CardSelectionContext(this);
        }

        public string EventId => _context.EventId;

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await _context.InitializeAsync(ct);

            var progressData = await EnsureEventDataInitializedAsync(ct);
            var allDefinitions = _context.GetCardDefinitions();
            
            _groupCompletionTracker = new GroupCompletionTracker(allDefinitions, progressData);
            _isCollectionCompleted = _groupCompletionTracker.IsAllGroupsCompleted;
        }
        
        private async UniTask<EventCardsSaveData> EnsureEventDataInitializedAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var data = await _context.LoadAsync(_context.EventId, ct);

            if (data != null && data.Cards != null && data.Cards.Count > 0)
            {
                return data;
            }

            var initializedData = new EventCardsSaveData
            {
                EventId = _context.EventId,
                // Preserve existing progress fields when cards were not yet expanded.
                Points = data?.Points ?? 0,
                Version = data?.Version ?? 1
            };
            foreach (var cardDefinition in _context.GetCardDefinitions())
            {
                initializedData.Cards.Add(new CardProgressData
                {
                    CardId = cardDefinition.Id,
                    IsUnlocked = false
                });
            }

            await _context.SaveAsync(initializedData, ct);
            return initializedData;
        }

        public List<CardPack> GetAllPacks() => _context.GetAllPacks();

        public CardPack GetPackById(string packId) => _context.GetPackById(packId);

        public async UniTask<List<string>> OpenPackAndUnlockAsync(string packId, CancellationToken ct = default)
        {
            var pack = _context.GetPackById(packId);

            return await OpenPackAndUnlockAsync(pack, ct);
        }

        public async UniTask<List<string>> OpenPackAndUnlockAsync(CardPack cardPack, CancellationToken ct = default)
        {
            if (cardPack == null)
            {
                return new List<string>();
            }
            
            var cardIds = await _context.GetRandomNewCardsAsync(cardPack, _selectionContext, ct);
            if (cardIds.Count > 0)
            {
                await AwardDuplicateCardPointsAsync(cardPack, cardIds, ct);
                await _context.UnlockCardsAsync(_context.EventId, cardIds, ct);
                NotifyCompletedGroups(cardIds);
                NotifyCollectionCompleted();
            }

            return cardIds;
        }

        private async UniTask AwardDuplicateCardPointsAsync(CardPack cardPack, List<string> openedCardIds, CancellationToken ct)
        {
            if (openedCardIds == null || openedCardIds.Count == 0)
            {
                return;
            }

            var openedCardsProgress = await _context.GetCardsByIdsAsync(_context.EventId, openedCardIds, ct);
            var duplicatePoints = _context.CalculateDuplicatePoints(openedCardIds, openedCardsProgress);
            if (!duplicatePoints.HasPoints)
            {
                return;
            }

            await AddPointsAsync(duplicatePoints.TotalPoints, ct);

            Debug.Log(
                $"[CardCollectionModule] Added {duplicatePoints.TotalPoints} duplicate-card points after opening pack '{cardPack.PackId}'. " +
                $"Cards: {string.Join(", ", duplicatePoints.AwardedCards)}");
        }

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default)
        {
            return _context.GetCardsByIdsAsync(_context.EventId, cardIds, ct);
        }

        public UniTask ResetNewFlagAsync(string cardId, CancellationToken ct = default)
        {
            return _context.ResetNewFlagAsync(_context.EventId, cardId, ct);
        }

        public UniTask ResetNewFlagsAsync(IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            return _context.ResetNewFlagsAsync(_context.EventId, cardIds, ct);
        }

        internal async UniTask AddPointsAsync(int pointsToAdd, CancellationToken ct = default)
        {
            await _context.AddPointsAsync(_context.EventId, pointsToAdd, ct);
        }

        public async UniTask<bool> TryAddPointsAsync(int pointsToAdd, CancellationToken ct = default)
        {
            await AddPointsAsync(pointsToAdd, ct);
            return true;
        }

        public UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default)
        {
            return _context.TrySpendPointsAsync(_context.EventId, pointsToSpend, ct);
        }
        
        public void Dispose()
        {
            _context.Dispose();
        }
        
        #region ICardCollectionUpdater implementation
        
        public async UniTask UnlockCard(string cardId, CancellationToken ct = default)
        {
            //TODO add here duplicate points. move from OpenPackAndUnlockAsync ? 
            try
            {
                await _context.UnlockCardAsync(_context.EventId, cardId, ct);
                NotifyCompletedGroups(new[] { cardId });
                NotifyCollectionCompleted();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to unlock card {cardId} for event {_context.EventId}: {ex}");
            }
        }
        
        public async UniTask UnlockCards(IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (cardIds == null || cardIds.Count == 0)
            {
                return;
            }

            try
            {
                await _context.UnlockCardsAsync(_context.EventId, cardIds, ct);
                NotifyCompletedGroups(cardIds);
                NotifyCollectionCompleted();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"[CardCollectionSaveController] Failed to unlock cards for event {_context.EventId}: {ex}");
            }
        }

        public async UniTask Save(CancellationToken ct = default)
        {
            try
            {
                var cardCollectionData = new EventCardsSaveData { EventId = _context.EventId };

                foreach (var cardCollectionConfig in _context.GetCardDefinitions())
                {
                    var cardData = new CardProgressData { CardId = cardCollectionConfig.Id, IsUnlocked = false };
                    cardCollectionData.Cards.Add(cardData);
                }

                await _context.SaveAsync(cardCollectionData, ct);
                _groupCompletionTracker?.ResetFromProgress(cardCollectionData);
                _isCollectionCompleted = _groupCompletionTracker?.IsAllGroupsCompleted == true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to save event {_context.EventId}: {ex}");
            }
        }

        public async UniTask<EventCardsSaveData> Load(CancellationToken ct = default)
        {
            try
            {
                return await _context.LoadAsync(_context.EventId, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to load event {_context.EventId}: {ex}");
            }
        }

        public async UniTask<HashSet<string>> GetMissingCardIdsAsync(List<CardDefinition> allCards, CancellationToken ct = default)
        {
            try
            {
                return await _context.GetMissingCardIdsAsync(_context.EventId, allCards, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionModule] Failed to get missing card IDs: {ex}");
            }
        }

        public async UniTask<int> GetCollectionPoints()
        {
            return await _context.GetPoints(_context.EventId);
        }

        public async UniTask Clear(CancellationToken ct = default)
        {
            try
            {
                await _context.ClearCollectionAsync(ct);
                var emptyData = new EventCardsSaveData { EventId = _context.EventId };
                _groupCompletionTracker?.ResetFromProgress(emptyData);
                _isCollectionCompleted = _groupCompletionTracker?.IsAllGroupsCompleted == true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to clear collection: {ex}");
            }
        }
        
        #endregion

        private void NotifyCompletedGroups(IReadOnlyCollection<string> openedCardIds)
        {
            if (_groupCompletionTracker == null || openedCardIds == null || openedCardIds.Count == 0)
            {
                return;
            }

            var completedGroupIds = _groupCompletionTracker.RegisterOpenedCards(openedCardIds);
            if (completedGroupIds == null || completedGroupIds.Count == 0)
                return;

            var items = completedGroupIds
                .Select(id => new CardGroupCompletedData { GroupType = id })
                .ToList();

            OnGroupCompleted?.Invoke(new CardGroupsCompletedData(items));
        }

        private void NotifyCollectionCompleted()
        {
            if (_groupCompletionTracker == null || _isCollectionCompleted)
            {
                return;
            }

            if (_groupCompletionTracker.IsAllGroupsCompleted)
            {
                _isCollectionCompleted = true;
                OnCollectionCompleted?.Invoke(new CardCollectionCompletedData
                {
                    EventId = _context.EventId,
                });
            }
        }
    }
}
