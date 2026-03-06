using CardCollection.Core;
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

        public void OpenCardCollectionWindow(
            ICardCollectionModule  cardCollectionModule,
            EventCardsSaveData  eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory  rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount)
        {
            var hasPreviousCollectedSnapshot = _collectionProgressSnapshotService.TryGetSnapshot(out CollectionProgressSnapshot previousSnapshot);
            var args = new CardCollectionArgs(
                _uiManager,
                cardCollectionModule,
                eventCardsSaveData,
                exchangeOfferProvider,
                rewardDefinitionFactory, 
                cardCollectionPointsAccount,
                previousSnapshot);
            _uiManager.Show<CardCollectionController>(args);

            _collectionProgressSnapshotService.SetSnapshot(eventCardsSaveData);
        }
    }
}