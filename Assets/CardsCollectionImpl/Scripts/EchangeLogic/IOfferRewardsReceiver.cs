using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface IOfferRewardsReceiver
    {
        UniTask<bool> ReceiveRewardsAsync(CollectionRewardDefinition collectionRewardDefinition, CancellationToken ct = default);
    }
}
