using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;

namespace CardCollectionImpl
{
    public class CardCollectionWindowPresenter : IWindowPresenter
    {
        private readonly UIManager _uiManager;
        private readonly ICollectionProgressSnapshotService _collectionProgressSnapshotService;
        
        public CardCollectionWindowPresenter(
            UIManager uiManager, 
            ICollectionProgressSnapshotService collectionProgressSnapshotService,
            EventCardsSaveData eventCardsSaveData = null)
        {
            _uiManager = uiManager;
            _collectionProgressSnapshotService = collectionProgressSnapshotService;

            if (eventCardsSaveData != null)
            {
                _collectionProgressSnapshotService.SetSnapshot(eventCardsSaveData);
            }
        }

        public bool OpenWindow(string windowId, object args)
        {
            return false;
        }

        public void OpenNewCardWindow(CardPack pack, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader)
        {
            var args = new NewCardArgs(pack, _uiManager, cardCollectionModule, cardCollectionReader);
            _uiManager.Show<NewCardController>(args);
        }

        public async UniTask OpenCardCollectionWindow(
            ICardCollectionModule  cardCollectionModule,
            EventCardsSaveData  eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory  rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CancellationToken ct)
        {
            var newCardsData = CardCollectionNewCardsDto.Create(eventCardsSaveData);
            var newCardIds = newCardsData.NewCardIds;

            if (newCardIds.Count > 0)
            {
                await cardCollectionModule.ResetNewFlagsAsync(newCardIds, ct);
            }

            _collectionProgressSnapshotService.TryGetSnapshot(out var snapshot);
            var args = new CardCollectionArgs(
                _uiManager,
                newCardsData,
                eventCardsSaveData,
                exchangeOfferProvider,
                rewardDefinitionFactory, 
                cardCollectionPointsAccount,
                snapshot);
            _uiManager.Show<CardCollectionController>(args);

            _collectionProgressSnapshotService.SetSnapshot(eventCardsSaveData);
        }
    }
}