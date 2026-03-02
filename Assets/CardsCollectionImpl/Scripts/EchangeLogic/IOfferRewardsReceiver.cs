using System.Threading;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;

namespace core
{
    public interface IOfferRewardsReceiver
    {
        UniTask<bool> ReceiveRewardsAsync(OfferContent offerContent, CancellationToken ct = default);
    }
}
