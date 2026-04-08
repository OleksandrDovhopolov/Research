using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class OpenPackFlow : IOpenPackFlow
    {
        private readonly UIManager _uiManager;
        private readonly ICardCollectionModule _collectionModule;
        private readonly ICardCollectionRewardHandler _rewardHandler;
        private readonly ICardCollectionWindowCoordinator _windowCoordinator;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;
        private readonly IPendingGroupCompletionPresentationQueue _groupCompletionPresentationQueue;

        public OpenPackFlow(
            UIManager uiManager,
            ICardCollectionModule collectionModule,
            ICardCollectionRewardHandler rewardHandler,
            ICardCollectionWindowCoordinator windowCoordinator,
            ICardCollectionCacheService cardCollectionCacheService,
            IPendingGroupCompletionPresentationQueue groupCompletionPresentationQueue)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _rewardHandler = rewardHandler ?? throw new ArgumentNullException(nameof(rewardHandler));
            _collectionModule = collectionModule ?? throw new ArgumentNullException(nameof(collectionModule));
            _windowCoordinator = windowCoordinator ?? throw new ArgumentNullException(nameof(windowCoordinator));
            _cardCollectionCacheService = cardCollectionCacheService ?? throw new ArgumentNullException(nameof(cardCollectionCacheService));
            _groupCompletionPresentationQueue = groupCompletionPresentationQueue ?? throw new ArgumentNullException(nameof(groupCompletionPresentationQueue));
        }

        public async UniTask OpenPackById(string packId, CancellationToken ct)
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
            var args = new NewCardArgs(_collectionModule.EventId, openPackResult.CurrentPoints, displayData);
            ct.ThrowIfCancellationRequested();
            _windowCoordinator.ShowNewCard(args);
            
            await UniTask.WaitUntil(() => _uiManager.IsWindowShown<NewCardController>(), cancellationToken: ct);
            await UniTask.WaitUntil(() => !_uiManager.IsWindowShown<NewCardController>(), cancellationToken: ct);
            
            await ShowPendingGroupCompletedAsync(ct);
        }

        private async UniTask ShowPendingGroupCompletedAsync(CancellationToken ct)
        {
            var completedGroups = _groupCompletionPresentationQueue.DequeueAll();
            if (completedGroups.Count == 0)
            {
                return;
            }

            var collectionData = await _collectionModule.Load(ct);
            var args = new CardGroupCollectionArgs(_collectionModule.EventId, collectionData, completedGroups, _rewardHandler);
            ct.ThrowIfCancellationRequested();
            _windowCoordinator.ShowGroupCompleted(args);
        }
    }
}