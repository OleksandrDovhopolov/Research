using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public sealed class CardCollectionModule : ICardCollectionModule, ICardCollectionUpdater, IDisposable
    {
        private readonly CardCollectionContext _context;

        public CardCollectionModule(CardCollectionModuleConfig  config)
        {
            _context = new CardCollectionContext(config);
        }

        public UniTask InitializeAsync() => _context.InitializeAsync();

        public List<CardPack> GetAllPacks() => _context.CardPackService.GetAllPacks();

        public CardPack GetPackById(string packId) => _context.CardPackService.GetPackById(packId);

        public async UniTask<List<string>> OpenPackAndUnlockAsync(string packId)
        {
            var pack = _context.CardPackService.GetPackById(packId);

            return await OpenPackAndUnlockAsync(pack);
        }

        public async UniTask<List<string>> OpenPackAndUnlockAsync(CardPack cardPack)
        {
            if (cardPack == null)
            {
                return new List<string>();
            }
            
            var cardIds = await _context.CardRandomizer.GetRandomNewCardsAsync(cardPack);
            if (cardIds.Count > 0)
            {
                await _context.CardProgressService.UnlockCardsAsync(_context.DefaultEventId, cardIds);
            }

            return cardIds;
        }

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds)
        {
            return _context.CardProgressService.GetCardsByIdsAsync(_context.DefaultEventId, cardIds);
        }

        public UniTask ResetNewFlagAsync(string cardId)
        {
            return _context.CardProgressService.ResetNewFlagAsync(_context.DefaultEventId, cardId);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
        
        #region ICardCollectionUpdater implementation
        
        public async UniTask UnlockCard(string cardId)
        {
            try
            {
                await _context.CardProgressService.UnlockCardAsync(_context.DefaultEventId, cardId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to unlock card {cardId} for event {_context.DefaultEventId}: {ex}");
            }
        }

        public async UniTask Save()
        {
            try
            {
                var cardCollectionData = new EventCardsSaveData { EventId = _context.DefaultEventId };

                foreach (var cardCollectionConfig in _context.CardDefinitionProvider.GetCardDefinitions())
                {
                    var cardData = new CardProgressData { CardId = cardCollectionConfig.Id, IsUnlocked = false };
                    cardCollectionData.Cards.Add(cardData);
                }

                await _context.CardProgressService.SaveAsync(cardCollectionData);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to save event {_context.DefaultEventId}: {ex}");
            }
        }

        public async UniTask<EventCardsSaveData> Load()
        {
            try
            {
                return await _context.CardProgressService.LoadAsync(_context.DefaultEventId);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to load event {_context.DefaultEventId}: {ex}");
            }
        }

        public async UniTask Clear()
        {
            try
            {
                await _context.CardProgressService.ClearCollectionAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[CardCollectionSaveController] Failed to clear collection: {ex}");
            }
        }
        
        #endregion
    }
}

