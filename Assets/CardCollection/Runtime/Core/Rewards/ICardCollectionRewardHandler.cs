using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardCollectionRewardHandler
    {
        UniTask InitializeAsync(CancellationToken ct = default);
        bool TryHandleGroupCompleted(CardGroupCompletedData groupCompletedData);
        bool TryHandleCollectionCompleted(CardCollectionCompletedData collectionCompletedData);
        bool TryHandleBuyPointsOffer(string offerId);
    }
}
