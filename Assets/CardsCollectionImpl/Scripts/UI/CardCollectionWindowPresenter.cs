using CardCollection.Core;
using UISystem;

namespace CardCollectionImpl
{
    public class CardCollectionWindowPresenter : IWindowPresenter
    {
        private readonly UIManager _uiManager;
        
        public CardCollectionWindowPresenter(UIManager uiManager)
        {
            _uiManager = uiManager;
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
            var args = new CardCollectionArgs(
                _uiManager,
                cardCollectionModule,
                eventCardsSaveData,
                exchangeOfferProvider,
                rewardDefinitionFactory, 
                cardCollectionPointsAccount);
            _uiManager.Show<CardCollectionController>(args);
        }
    }
}