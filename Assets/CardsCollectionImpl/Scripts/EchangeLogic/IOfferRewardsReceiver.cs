using System.Threading;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;

namespace core
{
    public interface IOfferRewardsReceiver
    {
        UniTask<bool> ReceiveRewardsAsync(CollectionRewardDefinition collectionRewardDefinition, CancellationToken ct = default);
    }
}
