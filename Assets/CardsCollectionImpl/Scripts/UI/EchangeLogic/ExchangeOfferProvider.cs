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
        
        private readonly Dictionary<string, ExchangePackEntry> _packById;
        
        public ExchangeOfferProvider(ExchangePacksConfig packsConfig, ICardCollectionRewardHandler cardCollectionRewardHandler)
        {
            _packById = new Dictionary<string, ExchangePackEntry>();
            _cardCollectionRewardHandler = cardCollectionRewardHandler;

            if (packsConfig == null || packsConfig.Packs == null)
            {
                return;
            }

            foreach (var pack in packsConfig.Packs)
            {
                if (pack == null || string.IsNullOrWhiteSpace(pack.Id))
                {
                    continue;
                }

                _packById[pack.Id] = pack;
            }
        }

        public IReadOnlyCollection<ExchangeOfferData> GetAllOffers()
        {
            return _packById.Values
                .Select(pack => new ExchangeOfferData(pack.Id, pack.Sprite, pack.PackPrice))
                .ToArray();
        }

        public int GetOfferPrice(string offerPackId)
        {
            return TryGetPackEntry(offerPackId, out var pack) ? pack.PackPrice : 0;
        }

        public async UniTask<bool> ReceiveOfferContent(string offerPackId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            
            return await _cardCollectionRewardHandler.TryHandleBuyPointsOffer(offerPackId, ct);
        }

        private bool TryGetPackEntry(string packId, out ExchangePackEntry pack)
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