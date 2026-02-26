using System.Threading;
using Cysharp.Threading.Tasks;

namespace core
{
    public interface IOfferRewardsReceiver
    {
        UniTask<bool> ReceiveRewardsAsync(OfferContent offerContent, CancellationToken ct = default);
    }
}
