using System;
using System.Collections.Generic;
using CardCollection.Core;
using Resources.Core;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionImplCompositionRoot : ICardCollectionCompositionRoot
    {
        private readonly UIManager _uiManager;
        private readonly ResourceManager _resourceManager;
        private readonly ExchangePacksConfig _exchangePacksConfig;
        private readonly ICollectionProgressSnapshotService _collectionProgressSnapshotService;

        public CardCollectionImplCompositionRoot(
            UIManager uiManager,
            ResourceManager resourceManager,
            ExchangePacksConfig exchangePacksConfig)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _exchangePacksConfig = exchangePacksConfig ?? throw new ArgumentNullException(nameof(exchangePacksConfig));
            _collectionProgressSnapshotService = new CollectionProgressSnapshotService();
            
            /*_iWindowPresenter ??= new CardCollectionWindowPresenter(
                _uiManager, _collectionProgressSnapshotService, eventCardsSaveData);*/
        }

        public IOfferRewardsReceiver CreateOfferRewardsReceiver()
        {
            return new OfferRewardsReceiver(_resourceManager);
        }

        public IRewardDefinitionFactory CreateRewardDefinitionFactory(List<CardPackConfig> cardPackConfigs)
        {
            return new RewardDefinitionFactory(_exchangePacksConfig, cardPackConfigs);
        }

        public ICardCollectionRewardHandler CreateRewardHandler(
            IOfferRewardsReceiver offerRewardsReceiver,
            IRewardDefinitionFactory rewardDefinitionFactory)
        {
            return new CardCollectionRewardHandler(offerRewardsReceiver, rewardDefinitionFactory);
        }

        public IExchangeOfferProvider CreateExchangeOfferProvider(ICardCollectionRewardHandler rewardHandler)
        {
            return new ExchangeOfferProvider(_exchangePacksConfig, rewardHandler, _uiManager);
        }

        public CardCollectionModuleConfig CreateModuleConfig(ICardPackProvider cardPackProvider, string eventId)
        {
            IEventCardsStorage cardsStorage = new JsonEventCardsStorage();
            ICardDefinitionProvider cardDefinitionProvider = new DefaultCardDefinitionProvider();
            ICardSelector cardSelector = new ProbabilityBasedCardSelector(PackRulesConfig.CreateDefaultRules());

            return new CardCollectionModuleConfig(
                cardPackProvider,
                cardsStorage,
                cardDefinitionProvider,
                cardSelector,
                CardsCollectionPointsCalculator.Instance,
                eventId);
        }

        private IWindowPresenter _iWindowPresenter;
        public IWindowPresenter CreateWindowPresenter(EventCardsSaveData eventCardsSaveData = null)
        {
            _iWindowPresenter ??= new CardCollectionWindowPresenter
                (_uiManager, _collectionProgressSnapshotService, eventCardsSaveData);
            return _iWindowPresenter;
        }
    }
}
