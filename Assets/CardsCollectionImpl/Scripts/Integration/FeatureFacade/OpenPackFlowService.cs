using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public sealed class OpenPackFlowService : IOpenPackFlowService
    {
        private readonly ICardCollectionModule _collectionModule;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;

        public OpenPackFlowService(ICardCollectionModule collectionModule, ICardCollectionCacheService cardCollectionCacheService)
        {
            _collectionModule = collectionModule ?? throw new ArgumentNullException(nameof(collectionModule));
            _cardCollectionCacheService = cardCollectionCacheService ?? throw new ArgumentNullException(nameof(cardCollectionCacheService));
        }

        public async UniTask<NewCardScreenData> LoadAsync(string packId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(packId))
            {
                throw new ArgumentException("Pack id cannot be null or whitespace.", nameof(packId));
            }

            var openPackResult = await _collectionModule.OpenPackAsync(packId, ct);
            var cardIds = openPackResult.OpenedCardIds as List<string> ?? new List<string>(openPackResult.OpenedCardIds);
            var cardsData = await _collectionModule.GetCardsByIdsAsync(cardIds, ct);
            var displayData = _cardCollectionCacheService.ToNewCardDisplayData(cardsData);

            return new NewCardScreenData(_collectionModule.EventId, openPackResult.CurrentPoints, displayData);
        }
    }
}
