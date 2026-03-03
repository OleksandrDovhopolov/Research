namespace CardCollection.Core
{
    public interface IWindowPresenter
    {
        bool OpenWindow(string windowId, object args);
        void OpenNewCardWindow(CardPack pack, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader);

        void OpenCardCollectionWindow(
            ICardCollectionModule cardCollectionModule,
            EventCardsSaveData eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount);
    }
}