using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IWindowPresenter
    {
        void OpenNewCardWindow(string packId, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader);
        void OpenNewCardWindow(CardPack pack, ICardCollectionModule cardCollectionModule, ICardCollectionReader cardCollectionReader);

        UniTask OpenCardCollectionWindow(
            ICardCollectionModule cardCollectionModule,
            ICardCollectionReader cardCollectionReader,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CancellationToken ct);
    }
}