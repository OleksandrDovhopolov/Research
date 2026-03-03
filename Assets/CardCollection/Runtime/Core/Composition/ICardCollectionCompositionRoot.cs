using System.Collections.Generic;

namespace CardCollection.Core
{
    public interface ICardCollectionCompositionRoot
    {
        IWindowPresenter CreateWindowPresenter();
        IOfferRewardsReceiver CreateOfferRewardsReceiver(ICardCollectionResourceContext resourceContext);
        IRewardDefinitionFactory CreateRewardDefinitionFactory(ICardCollectionExchangeConfigContext exchangeConfigContext, List<CardPackConfig> cardPackConfigs);
        ICardCollectionRewardHandler CreateRewardHandler(IOfferRewardsReceiver offerRewardsReceiver, IRewardDefinitionFactory rewardDefinitionFactory);
        IExchangeOfferProvider CreateExchangeOfferProvider(ICardCollectionExchangeConfigContext exchangeConfigContext, ICardCollectionRewardHandler rewardHandler);
        CardCollectionModuleConfig CreateModuleConfig(ICardPackProvider cardPackProvider, string eventId);
    }
}