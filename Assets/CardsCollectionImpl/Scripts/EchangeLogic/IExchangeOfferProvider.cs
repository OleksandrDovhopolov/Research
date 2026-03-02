using System.Collections.Generic;
using System.Threading;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;

namespace core
{
    public interface IExchangeOfferProvider
    {
        IReadOnlyCollection<ExchangePackEntry> GetAllOffers();
        int GetOfferPrice(string offerPackId);
        UniTask<bool> ReceiveOfferContent(string offerPackId, CancellationToken ct = default);
        UniTask<bool> TrySpendCollectionPointsAsync(int pointsToSpend, CancellationToken ct = default);
        
        UniTask<OfferContent> GetOfferContentAsync(string offerPackId, CancellationToken ct = default);
        UniTask<OfferContent> GetCollectionRewardData(CancellationToken ct = default);
    }
}
