using System.Collections.Generic;

namespace CardCollection.Core
{
    public interface ICardCollectionCompositionRoot
    {
        CardCollectionModuleConfig CreateModuleConfig(ICardPackProvider cardPackProvider, string eventId);
        IRewardDefinitionFactory CreateRewardDefinitionFactory(List<CardPackConfig> cardPackConfigs);
        IExchangeOfferProvider CreateExchangeOfferProvider(ICardCollectionRewardHandler rewardHandler);
    }
}