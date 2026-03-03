using System;
using System.Collections.Generic;
using CardCollection.Core;
using Resources.Core;
using UISystem;

namespace CardCollectionImpl
{
    public sealed class CardCollectionImplCompositionRoot : ICardCollectionCompositionRoot
    {
        public IOfferRewardsReceiver CreateOfferRewardsReceiver(object resourceManager)
        {
            if (resourceManager is not ResourceManager typedResourceManager)
            {
                throw new ArgumentException("Expected ResourceManager instance.", nameof(resourceManager));
            }

            return new OfferRewardsReceiver(typedResourceManager);
        }

        public IRewardDefinitionFactory CreateRewardDefinitionFactory(object exchangePacksConfig, List<CardPackConfig> cardPackConfigs)
        {
            if (exchangePacksConfig is not ExchangePacksConfig typedExchangePacksConfig)
            {
                throw new ArgumentException("Expected ExchangePacksConfig instance.", nameof(exchangePacksConfig));
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
            object exchangePacksConfig,
            ICardCollectionRewardHandler rewardHandler,
            object uiManager)
        {
            if (exchangePacksConfig is not ExchangePacksConfig typedExchangePacksConfig)
            {
                throw new ArgumentException("Expected ExchangePacksConfig instance.", nameof(exchangePacksConfig));
            }

            if (rewardHandler is not CardCollectionRewardHandler typedRewardHandler)
            {
                throw new ArgumentException(
                    "Expected CardCollectionRewardHandler implementation from CardsCollectionImpl.",
                    nameof(rewardHandler));
            }

            if (uiManager is not UIManager typedUiManager)
            {
                throw new ArgumentException("Expected UIManager instance.", nameof(uiManager));
            }

            return new ExchangeOfferProvider(typedExchangePacksConfig, typedRewardHandler, typedUiManager);
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

        public IWindowPresenter CreateNewCardWindowPresenter(object uiManager)
        {
            if (uiManager is not UIManager typeUIManager)
            {
                throw new ArgumentException("Expected UIManager instance.", nameof(typeUIManager));
            }
            return new CardCollectionWindowPresenter(typeUIManager);
        }
    }
}
