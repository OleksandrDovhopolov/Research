using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UIShared;

namespace CardCollectionImpl
{
    public class ExchangeOfferProvider : IExchangeOfferProvider
    {
        private readonly ICardCollectionRewardHandler _cardCollectionRewardHandler;
        
        private readonly Dictionary<string, CardCollectionOfferConfig> _packById;
        
        public ExchangeOfferProvider(IReadOnlyList<CardCollectionOfferConfig> offerConfigs, ICardCollectionRewardHandler cardCollectionRewardHandler)
        {
            _packById = new Dictionary<string, CardCollectionOfferConfig>();
            _cardCollectionRewardHandler = cardCollectionRewardHandler;

            if (offerConfigs == null )
            {
                return;
            }

            foreach (var pack in offerConfigs)
            {
                if (pack == null || string.IsNullOrWhiteSpace(pack.id))
                {
                    continue;
                }

                _packById[pack.id] = pack;
            }
        }

        public IReadOnlyCollection<CardCollectionOfferConfig> GetAllOffers()
        {
            return _packById.Values.ToArray();
        }

        public int GetOfferPrice(string offerPackId)
        {
            return TryGetPackEntry(offerPackId, out var pack) ? pack.packPrice : 0;
        }

        public async UniTask<bool> ReceiveOfferContent(string offerPackId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            return await _cardCollectionRewardHandler.TryHandleBuyPointsOffer(offerPackId, ct);
        }

        private bool TryGetPackEntry(string packId, out CardCollectionOfferConfig pack)
        {
            if (string.IsNullOrWhiteSpace(packId))
            {
                pack = null;
                return false;
            }

            return _packById.TryGetValue(packId, out pack);
        }
    }
}