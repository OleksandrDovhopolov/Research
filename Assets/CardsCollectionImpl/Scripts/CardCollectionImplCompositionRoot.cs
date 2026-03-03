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

        public CardCollectionImplCompositionRoot(UIManager uiManager)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
        }

        public IOfferRewardsReceiver CreateOfferRewardsReceiver(ICardCollectionResourceContext resourceContext)
        {
            if (resourceContext is not CardCollectionResourceContext typedResourceContext ||
                typedResourceContext.ResourceManager is not ResourceManager typedResourceManager)
            {
                throw new ArgumentException(
                    $"Expected {nameof(CardCollectionResourceContext)} with a valid {nameof(ResourceManager)}.",
                    nameof(resourceContext));
            }

            return new OfferRewardsReceiver(typedResourceManager);
        }

        public IRewardDefinitionFactory CreateRewardDefinitionFactory(
            ICardCollectionExchangeConfigContext exchangeConfigContext,
            List<CardPackConfig> cardPackConfigs)
        {
            if (exchangeConfigContext is not CardCollectionExchangeConfigContext typedExchangeConfigContext ||
                typedExchangeConfigContext.ExchangePacksConfig is not ExchangePacksConfig typedExchangePacksConfig)
            {
                throw new ArgumentException(
                    $"Expected {nameof(CardCollectionExchangeConfigContext)} with a valid {nameof(ExchangePacksConfig)}.",
                    nameof(exchangeConfigContext));
            }

            return new RewardDefinitionFactory(typedExchangePacksConfig, cardPackConfigs);
        }

        public ICardCollectionRewardHandler CreateRewardHandler(
            IOfferRewardsReceiver offerRewardsReceiver,
            IRewardDefinitionFactory rewardDefinitionFactory)
        {
            return new CardCollectionRewardHandler(offerRewardsReceiver, rewardDefinitionFactory);
        }

        public IExchangeOfferProvider CreateExchangeOfferProvider(
            ICardCollectionExchangeConfigContext exchangeConfigContext,
            ICardCollectionRewardHandler rewardHandler)
        {
            if (exchangeConfigContext is not CardCollectionExchangeConfigContext typedExchangeConfigContext ||
                typedExchangeConfigContext.ExchangePacksConfig is not ExchangePacksConfig typedExchangePacksConfig)
            {
                throw new ArgumentException(
                    $"Expected {nameof(CardCollectionExchangeConfigContext)} with a valid {nameof(ExchangePacksConfig)}.",
                    nameof(exchangeConfigContext));
            }

            if (rewardHandler is not CardCollectionRewardHandler typedRewardHandler)
            {
                throw new ArgumentException(
                    "Expected CardCollectionRewardHandler implementation from CardsCollectionImpl.",
                    nameof(rewardHandler));
            }

            return new ExchangeOfferProvider(
                typedExchangePacksConfig,
                typedRewardHandler,
                _uiManager);
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

        public IWindowPresenter CreateWindowPresenter()
        {
            return new CardCollectionWindowPresenter(_uiManager);
        }
    }
}
