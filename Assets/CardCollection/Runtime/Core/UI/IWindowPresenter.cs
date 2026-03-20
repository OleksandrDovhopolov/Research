using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IWindowPresenter
    {
        UniTask OpenCardCollectionWindow(
            ICardCollectionModule cardCollectionModule,
            ICardCollectionReader cardCollectionReader,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CancellationToken ct);
    }
}