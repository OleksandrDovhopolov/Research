using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IWindowPresenter
    {
        void OpenNewCardWindow(string packId, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader, bool showAsync = false);
        void OpenNewCardWindow(CardPack pack, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader, bool showAsync = false);

        UniTask OpenCardCollectionWindow(
            ICardCollectionModule cardCollectionModule,
            EventCardsSaveData eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CancellationToken ct);
    }
}