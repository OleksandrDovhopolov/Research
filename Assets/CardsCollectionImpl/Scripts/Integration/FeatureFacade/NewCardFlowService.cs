using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public sealed class NewCardFlowService : INewCardFlowService
    {
        private readonly ICardCollectionModule _collectionModule;
        private readonly ICardCollectionPointsAccount _pointsAccount;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;

        public NewCardFlowService(
            ICardCollectionModule collectionModule,
            ICardCollectionPointsAccount pointsAccount,
            ICardCollectionCacheService cardCollectionCacheService)
        {
            _collectionModule = collectionModule ?? throw new ArgumentNullException(nameof(collectionModule));
            _pointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _cardCollectionCacheService = cardCollectionCacheService ?? throw new ArgumentNullException(nameof(cardCollectionCacheService));
        }

        public async UniTask<NewCardScreenData> LoadAsync(string packId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(packId))
            {
                throw new ArgumentException("Pack id cannot be null or whitespace.", nameof(packId));
            }

            var collectionPoints = await _pointsAccount.GetCollectionPoints(ct);
            var cardsIdList = await _collectionModule.OpenPackAndUnlockAsync(packId, ct);
            var cardsData = await _collectionModule.GetCardsByIdsAsync(cardsIdList, ct);
            var displayData = _cardCollectionCacheService.ToNewCardDisplayData(cardsData);

            return new NewCardScreenData(_collectionModule.EventId, collectionPoints, displayData);
        }
    }
}
