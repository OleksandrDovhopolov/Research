using System.Collections.Generic;

namespace CardCollection.Core
{
    public interface ICardCollectionCompositionRoot
    {
        IOfferRewardsReceiver CreateOfferRewardsReceiver(object resourceManager);
        IRewardDefinitionFactory CreateRewardDefinitionFactory(object exchangePacksConfig, List<CardPackConfig> cardPackConfigs);
        ICardCollectionRewardHandler CreateRewardHandler(IOfferRewardsReceiver offerRewardsReceiver, IRewardDefinitionFactory rewardDefinitionFactory);
        IExchangeOfferProvider CreateExchangeOfferProvider(
            object exchangePacksConfig,
            ICardCollectionRewardHandler rewardHandler,
            object uiManager);
        CardCollectionModuleConfig CreateModuleConfig(ICardPackProvider cardPackProvider, string eventId);
        
        IWindowPresenter CreateNewCardWindowPresenter(object uiManager);
    }
}
