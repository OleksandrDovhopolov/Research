using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    public sealed class CardCollectionModule : ICardCollectionModule, ICardCollectionReader, ICardCollectionUpdater, IDisposable
    {
        private readonly CardCollectionContext _context;
        private readonly CardSelectionContext _selectionContext;
        
        public CardCollectionModule(CardCollectionModuleConfig  config)
        {
            _context = new CardCollectionContext(config);
            _selectionContext = new CardSelectionContext(this);
        }

        public UniTask InitializeAsync(CancellationToken ct = default) => _context.InitializeAsync(ct);

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
                await _context.UnlockCardsAsync(_context.DefaultEventId, cardIds, ct);
            }

            return cardIds;
        }

        private async UniTask AwardDuplicateCardPointsAsync(CardPack cardPack, List<string> openedCardIds, CancellationToken ct)
        {
            if (openedCardIds == null || openedCardIds.Count == 0)
            {
                return;
            }

            var openedCardsProgress = await _context.GetCardsByIdsAsync(_context.DefaultEventId, openedCardIds, ct);
            var duplicatePoints = _context.CalculateDuplicatePoints(openedCardIds, openedCardsProgress);
            if (!duplicatePoints.HasPoints)
            {
                return;
            }

            await _context.AddPointsAsync(_context.DefaultEventId, duplicatePoints.TotalPoints, ct);

            Debug.Log(
                $"[CardCollectionModule] Added {duplicatePoints.TotalPoints} duplicate-card points after opening pack '{cardPack.PackId}'. " +
                $"Cards: {string.Join(", ", duplicatePoints.AwardedCards)}");
        }

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default)
        {
            return _context.GetCardsByIdsAsync(_context.DefaultEventId, cardIds, ct);
        }

        public UniTask ResetNewFlagAsync(string cardId, CancellationToken ct = default)
        {
            return _context.ResetNewFlagAsync(_context.DefaultEventId, cardId, ct);
        }
        
        public void Dispose()
        {
            _context.Dispose();
        }
        
        #region ICardCollectionUpdater implementation
        
        public async UniTask UnlockCard(string cardId, CancellationToken ct = default)
        {
            try
            {
                await _context.UnlockCardAsync(_context.DefaultEventId, cardId, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to unlock card {cardId} for event {_context.DefaultEventId}: {ex}");
            }
        }

        public async UniTask Save(CancellationToken ct = default)
        {
            try
            {
                var cardCollectionData = new EventCardsSaveData { EventId = _context.DefaultEventId };

                foreach (var cardCollectionConfig in _context.GetCardDefinitions())
                {
                    var cardData = new CardProgressData { CardId = cardCollectionConfig.Id, IsUnlocked = false };
                    cardCollectionData.Cards.Add(cardData);
                }

                await _context.SaveAsync(cardCollectionData, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to save event {_context.DefaultEventId}: {ex}");
            }
        }

        public async UniTask<EventCardsSaveData> Load(CancellationToken ct = default)
        {
            try
            {
                return await _context.LoadAsync(_context.DefaultEventId, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to load event {_context.DefaultEventId}: {ex}");
            }
        }

        public async UniTask<HashSet<string>> GetMissingCardIdsAsync(List<CardDefinition> allCards, CancellationToken ct = default)
        {
            try
            {
                return await _context.GetMissingCardIdsAsync(_context.DefaultEventId, allCards, ct);
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
            return await _context.GetPoints(_context.DefaultEventId);
        }

        public async UniTask Clear(CancellationToken ct = default)
        {
            try
            {
                await _context.ClearCollectionAsync(ct);
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
    }
}
