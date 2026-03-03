using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IOfferRewardsReceiver
    {
        UniTask<bool> ReceiveRewardsAsync(CollectionRewardDefinition collectionRewardDefinition, CancellationToken ct = default);
    }
}
