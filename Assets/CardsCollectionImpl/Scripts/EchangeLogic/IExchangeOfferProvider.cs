using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface IExchangeOfferProvider
    {
        IReadOnlyCollection<ExchangePackEntry> GetAllOffers();
        int GetOfferPrice(string offerPackId);
        UniTask<bool> ReceiveOfferContent(string offerPackId, CancellationToken ct = default);
    }
}
