using System;
using System.Collections.Generic;
using CardCollection.Core;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionImplCompositionRoot : ICardCollectionCompositionRoot
    {
        private readonly UIManager _uiManager;
        private readonly ExchangePacksConfig _exchangePacksConfig;
        private readonly ICollectionProgressSnapshotService _collectionProgressSnapshotService;

        public CardCollectionImplCompositionRoot(UIManager uiManager, ExchangePacksConfig exchangePacksConfig)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _exchangePacksConfig = exchangePacksConfig ?? throw new ArgumentNullException(nameof(exchangePacksConfig));
            _collectionProgressSnapshotService = new CollectionProgressSnapshotService();
        }

        public IRewardDefinitionFactory CreateRewardDefinitionFactory(List<CardPackConfig> cardPackConfigs)
        {
            return new RewardDefinitionFactory(_exchangePacksConfig, cardPackConfigs);
        }

        public IExchangeOfferProvider CreateExchangeOfferProvider(ICardCollectionRewardHandler rewardHandler)
        {
            return new ExchangeOfferProvider(_exchangePacksConfig, rewardHandler);
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
