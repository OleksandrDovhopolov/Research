using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class OpenPackFlow : IOpenPackFlow
    {
        private readonly ICardCollectionModule _collectionModule;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;
        private readonly ICardCollectionWindowCoordinator _windowCoordinator;

        public OpenPackFlow(
            ICardCollectionModule collectionModule,
            ICardCollectionCacheService cardCollectionCacheService,
            ICardCollectionWindowCoordinator windowCoordinator)
        {
            _collectionModule = collectionModule ?? throw new ArgumentNullException(nameof(collectionModule));
            _cardCollectionCacheService = cardCollectionCacheService ?? throw new ArgumentNullException(nameof(cardCollectionCacheService));
            _windowCoordinator = windowCoordinator ?? throw new ArgumentNullException(nameof(windowCoordinator));
        }

        public async UniTask TryOpenPackById(string packId, CancellationToken ct)
        {
            Debug.LogWarning($"[CardCollectionRuntime] TryShowNewCardWindow {packId}");
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(packId))
            {
                throw new ArgumentException("Pack id cannot be null or whitespace.", nameof(packId));
            }

            var openPackResult = await _collectionModule.OpenPackAsync(packId, ct);
            var cardIds = openPackResult.OpenedCardIds as List<string> ?? new List<string>(openPackResult.OpenedCardIds);
            var cardsData = await _collectionModule.GetCardsByIdsAsync(cardIds, ct);
            var displayData = _cardCollectionCacheService.ToNewCardDisplayData(cardsData);
            var screenData = new NewCardScreenData(_collectionModule.EventId, openPackResult.CurrentPoints, displayData);

            var args = new NewCardArgs(screenData);
            ct.ThrowIfCancellationRequested();
            _windowCoordinator.ShowNewCard(args);
        }
    }
}