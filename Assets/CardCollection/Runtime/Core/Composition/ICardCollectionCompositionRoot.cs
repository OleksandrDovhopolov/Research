using System.Collections.Generic;

namespace CardCollection.Core
{
    public interface ICardCollectionCompositionRoot
    {
        IWindowPresenter CreateWindowPresenter(EventCardsSaveData eventCardsSaveData = null);
        CardCollectionModuleConfig CreateModuleConfig(ICardPackProvider cardPackProvider, string eventId);
        IRewardDefinitionFactory CreateRewardDefinitionFactory(List<CardPackConfig> cardPackConfigs);
        IExchangeOfferProvider CreateExchangeOfferProvider(ICardCollectionRewardHandler rewardHandler);
    }
}