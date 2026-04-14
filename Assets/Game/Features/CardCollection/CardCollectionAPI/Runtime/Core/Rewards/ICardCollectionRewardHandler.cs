using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardCollectionRewardHandler
    {
        RewardViewData CreateRewardViewData(string groupType);
        UniTask<bool> TryHandleGroupCompleted(CardGroupCompletedData groupCompletedData, CancellationToken ct = default);
        UniTask<bool> TryHandleCollectionCompleted(CardCollectionCompletedData collectionCompletedData, CancellationToken ct = default);
        UniTask<bool> TryHandleBuyPointsOffer(string offerId, CancellationToken ct = default);
    }
}
