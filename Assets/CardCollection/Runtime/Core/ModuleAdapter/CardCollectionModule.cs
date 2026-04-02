using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

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
        
        private GroupCompletionTracker _groupCompletionTracker;
        private bool _isCollectionCompleted;
        
        public event Action<CardGroupsCompletedData> OnGroupCompleted;
        public event Action<CardCollectionCompletedData> OnCollectionCompleted;
        
        public CardCollectionModule(CardCollectionModuleConfig  config)
        {
            _context = new CardCollectionContext(config);
        }

        public string EventId => _context.EventId;

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await _context.InitializeAsync(ct);

            var progressData = await EnsureEventDataInitializedAsync(ct);
            var allDefinitions = _context.CardDefinitionProvider.GetCardDefinitions();
            
            _groupCompletionTracker = new GroupCompletionTracker(allDefinitions, progressData);
            _isCollectionCompleted = _groupCompletionTracker.IsAllGroupsCompleted;
        }
        
        private async UniTask<EventCardsSaveData> EnsureEventDataInitializedAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var data = await _context.ProgressQueryService.LoadAsync(_context.EventId, ct);

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
            foreach (var cardDefinition in _context.CardDefinitionProvider.GetCardDefinitions())
            {
                initializedData.Cards.Add(new CardProgressData
                {
                    CardId = cardDefinition.Id,
                    IsUnlocked = false
                });
            }

            await _context.CardProgressService.SaveAsync(initializedData, ct);
            return initializedData;
        }

        public CardPack GetPackById(string packId) => _context.CardPackService.GetPackById(packId);

        public async UniTask<List<string>> OpenPackAndUnlockAsync(string packId, CancellationToken ct = default)
        {
            var result = await _context.OpenPackUseCase.ExecuteAsync(_context.EventId, packId, ct);
            if (result.OpenedCardIds.Count > 0)
            {
                NotifyCompletedGroups(result.OpenedCardIds);
                NotifyCollectionCompleted();
            }

            return result.OpenedCardIds.ToList();
        }

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default)
        {
            return _context.ProgressQueryService.GetCardsByIdsAsync(_context.EventId, cardIds, ct);
        }

        public UniTask ResetNewFlagsAsync(IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            return _context.CardProgressService.ResetNewFlagsAsync(_context.EventId, cardIds, ct);
        }

        internal async UniTask AddPointsAsync(int pointsToAdd, CancellationToken ct = default)
        {
            await _context.PointsAccountService.TryAddAsync(_context.EventId, pointsToAdd, ct);
        }

        public async UniTask<bool> TryAddPointsAsync(int pointsToAdd, CancellationToken ct = default)
        {
            await AddPointsAsync(pointsToAdd, ct);
            return true;
        }

        public UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default)
        {
            return _context.PointsAccountService.TrySpendAsync(_context.EventId, pointsToSpend, ct);
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
                var result = await _context.UnlockCardsUseCase.ExecuteAsync(_context.EventId, new[] { cardId }, ct);
                NotifyCompletedGroups(result.NewlyUnlockedCardIds);
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
                var result = await _context.UnlockCardsUseCase.ExecuteAsync(_context.EventId, cardIds, ct);
                NotifyCompletedGroups(result.NewlyUnlockedCardIds);
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

                foreach (var cardCollectionConfig in _context.CardDefinitionProvider.GetCardDefinitions())
                {
                    var cardData = new CardProgressData { CardId = cardCollectionConfig.Id, IsUnlocked = false };
                    cardCollectionData.Cards.Add(cardData);
                }

                await _context.CardProgressService.SaveAsync(cardCollectionData, ct);
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
                return await _context.ProgressQueryService.LoadAsync(_context.EventId, ct);
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
                return await _context.ProgressQueryService.GetMissingCardIdsAsync(_context.EventId, allCards, ct);
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

        public async UniTask<int> GetCollectionPoints(CancellationToken ct = default)
        {
            return await _context.PointsAccountService.GetBalanceAsync(_context.EventId, ct);
        }

        public async UniTask Clear(CancellationToken ct = default)
        {
            try
            {
                await _context.CardProgressService.ClearCollectionAsync(ct);
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
