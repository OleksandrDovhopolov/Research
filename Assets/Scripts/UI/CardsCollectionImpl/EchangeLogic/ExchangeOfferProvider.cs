using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;

namespace core
{
    public class ExchangeOfferProvider : IExchangeOfferProvider
    {
        private readonly UIManager _uiManager;
        private readonly ICardCollectionPointsAccount _cardCollectionPointsAccount;
        private readonly IOfferRewardsReceiver _offerRewardsReceiver;
        private readonly IOfferDefinitionFactory _offerDefinitionFactory;
        
        private readonly Dictionary<string, ExchangePackEntry> _packById;
        
        public ExchangeOfferProvider(ExchangePacksConfig packsConfig,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            IOfferRewardsReceiver offerRewardsReceiver,
            UIManager uiManager, 
            IOfferDefinitionFactory offerDefinitionFactory)
        {
            _cardCollectionPointsAccount = cardCollectionPointsAccount;
            _offerRewardsReceiver = offerRewardsReceiver;
            _packById = new Dictionary<string, ExchangePackEntry>();
            _uiManager = uiManager;
            _offerDefinitionFactory = offerDefinitionFactory;

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

        public IReadOnlyCollection<ExchangePackEntry> GetAllOffers()
        {
            return _packById.Values.ToArray();
        }

        public int GetOfferPrice(string offerPackId)
        {
            return TryGetPackEntry(offerPackId, out var pack) ? pack.PackPrice : 0;
        }

        public async UniTask<bool> ReceiveOfferContent(string offerPackId, CancellationToken ct = default)
        {
            const string infoText = "Pack received successfully";
            var infoArgs = new InfoWidgetArg(_uiManager, infoText);
            _uiManager.Show<InfoWidgetController>(infoArgs);
            
            var offerContent = _offerDefinitionFactory.CreateFromOfferReward(offerPackId);
            return await _offerRewardsReceiver.ReceiveRewardsAsync(offerContent, ct);
        }

        public async UniTask<bool> TrySpendCollectionPointsAsync(int pointsToSpend, CancellationToken ct = default)
        {
            return await _cardCollectionPointsAccount.TrySpendPointsAsync(pointsToSpend, ct);
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