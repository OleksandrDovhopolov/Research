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

        public List<CardPack> GetAllPacks() => _context.CardPackService.GetAllPacks();

        public CardPack GetPackById(string packId) => _context.CardPackService.GetPackById(packId);

        public async UniTask<List<string>> OpenPackAndUnlockAsync(string packId, CancellationToken ct = default)
        {
            var pack = _context.CardPackService.GetPackById(packId);

            return await OpenPackAndUnlockAsync(pack, ct);
        }

        public async UniTask<List<string>> OpenPackAndUnlockAsync(CardPack cardPack, CancellationToken ct = default)
        {
            if (cardPack == null)
            {
                return new List<string>();
            }
            
            var cardIds = await _context.CardRandomizer.GetRandomNewCardsAsync(cardPack, _selectionContext, ct);
            if (cardIds.Count > 0)
            {
                await AwardDuplicateCardPointsAsync(cardPack, cardIds, ct);
                await _context.CardProgressService.UnlockCardsAsync(_context.DefaultEventId, cardIds, ct);
            }

            return cardIds;
        }

        private async UniTask AwardDuplicateCardPointsAsync(CardPack cardPack, List<string> openedCardIds, CancellationToken ct)
        {
            if (openedCardIds == null || openedCardIds.Count == 0)
            {
                return;
            }

            var openedCardsProgress = await _context.CardProgressService.GetCardsByIdsAsync(_context.DefaultEventId, openedCardIds, ct);
            var cardProgressById = new Dictionary<string, CardProgressData>(openedCardsProgress.Count);
            foreach (var cardProgress in openedCardsProgress)
            {
                if (string.IsNullOrEmpty(cardProgress.CardId))
                {
                    continue;
                }

                // Last write wins to avoid failing on malformed save data with duplicate IDs.
                cardProgressById[cardProgress.CardId] = cardProgress;
            }

            if (cardProgressById.Count == 0)
            {
                return;
            }

            var allCardDefinitions = _context.CardDefinitionProvider.GetCardDefinitions();
            var cardDefinitionsById = new Dictionary<string, CardDefinition>(allCardDefinitions.Count);
            foreach (var cardDefinition in allCardDefinitions)
            {
                if (string.IsNullOrEmpty(cardDefinition.Id))
                {
                    continue;
                }

                cardDefinitionsById[cardDefinition.Id] = cardDefinition;
            }

            var totalPointsToAdd = 0;
            var duplicateCardPointsLog = new List<string>();

            foreach (var cardId in openedCardIds)
            {
                ct.ThrowIfCancellationRequested();

                if (!cardProgressById.TryGetValue(cardId, out var progressData) || !progressData.IsUnlocked)
                {
                    continue;
                }

                if (!cardDefinitionsById.TryGetValue(cardId, out var cardDefinition))
                {
                    Debug.LogWarning($"[CardCollectionModule] Duplicate card definition not found for card ID: {cardId}");
                    continue;
                }

                var pointsForCard = GetDuplicateCardPoints(cardDefinition);
                if (pointsForCard <= 0)
                {
                    continue;
                }

                totalPointsToAdd += pointsForCard;
                duplicateCardPointsLog.Add($"{cardId}(+{pointsForCard})");
            }

            if (totalPointsToAdd <= 0)
            {
                return;
            }

            await _context.CardProgressService.AddPointsAsync(_context.DefaultEventId, totalPointsToAdd, ct);

            Debug.Log(
                $"[CardCollectionModule] Added {totalPointsToAdd} duplicate-card points after opening pack '{cardPack.PackId}'. " +
                $"Cards: {string.Join(", ", duplicateCardPointsLog)}");
        }

        private static int GetDuplicateCardPoints(CardDefinition cardDefinition)
        {
            if (cardDefinition == null)
            {
                return 0;
            }

            if (cardDefinition.PremiumCard)
            {
                return 10;
            }

            return cardDefinition.Stars switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                4 => 5,
                5 => 10,
                _ => 0
            };
        }

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default)
        {
            return _context.CardProgressService.GetCardsByIdsAsync(_context.DefaultEventId, cardIds, ct);
        }

        public UniTask ResetNewFlagAsync(string cardId, CancellationToken ct = default)
        {
            return _context.CardProgressService.ResetNewFlagAsync(_context.DefaultEventId, cardId, ct);
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
                await _context.CardProgressService.UnlockCardAsync(_context.DefaultEventId, cardId, ct);
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

                foreach (var cardCollectionConfig in _context.CardDefinitionProvider.GetCardDefinitions())
                {
                    var cardData = new CardProgressData { CardId = cardCollectionConfig.Id, IsUnlocked = false };
                    cardCollectionData.Cards.Add(cardData);
                }

                await _context.CardProgressService.SaveAsync(cardCollectionData, ct);
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
                return await _context.CardProgressService.LoadAsync(_context.DefaultEventId, ct);
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
                return await _context.CardProgressService.GetMissingCardIdsAsync(_context.DefaultEventId, allCards, ct);
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

        public async UniTask Clear(CancellationToken ct = default)
        {
            try
            {
                await _context.CardProgressService.ClearCollectionAsync(ct);
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
