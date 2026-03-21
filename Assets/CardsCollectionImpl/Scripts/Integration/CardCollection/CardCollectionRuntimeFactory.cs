using System;
using CardCollection.Core;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionRuntimeFactory
    {
        private readonly UIManager _uiManager;
        private readonly ICardCollectionModule _module;
        private readonly ICardCollectionReader _reader;
        private readonly ICardCollectionPointsAccount _pointsAccount;
        private readonly IExchangeOfferProvider _exchangeOfferProvider;
        private readonly IRewardDefinitionFactory _rewardDefinitionFactory;
        private readonly CollectionProgressSnapshotService _snapshotService;

        private ICardCollectionWindowOpener _cardCollectionWindowOpener;
        
        public CardCollectionRuntimeFactory(
            UIManager uiManager,
            ICardCollectionModule module,
            ICardCollectionReader reader,
            ICardCollectionPointsAccount pointsAccount,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            CollectionProgressSnapshotService snapshotService)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _pointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
            _exchangeOfferProvider = exchangeOfferProvider ?? throw new ArgumentNullException(nameof(exchangeOfferProvider));
            _rewardDefinitionFactory = rewardDefinitionFactory ?? throw new ArgumentNullException(nameof(rewardDefinitionFactory));
        }

        public ICardCollectionWindowOpener CreateCardPackWindowOpener()
        {
            if (_cardCollectionWindowOpener != null)
            {
                return _cardCollectionWindowOpener;
            }
            
            _cardCollectionWindowOpener = new CardCollectionWindowOpener(
                _uiManager, 
                _module, 
                _reader, 
                _pointsAccount,
                _exchangeOfferProvider,
                _rewardDefinitionFactory,
                _snapshotService);
            return _cardCollectionWindowOpener;
        }
    }
}
