using System;
using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionImplCompositionRoot : ICardCollectionCompositionRoot
    {
        private readonly ExchangePacksConfig _exchangePacksConfig;

        public CardCollectionImplCompositionRoot(ExchangePacksConfig exchangePacksConfig)
        {
            _exchangePacksConfig = exchangePacksConfig ?? throw new ArgumentNullException(nameof(exchangePacksConfig));
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
    }
}
