using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IExchangeOfferProvider
    {
        IReadOnlyCollection<ExchangeOfferData> GetAllOffers();
        int GetOfferPrice(string offerPackId);
        UniTask<bool> ReceiveOfferContent(string offerPackId, CancellationToken ct = default);
    }
}
