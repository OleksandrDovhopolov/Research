using System.Collections.Generic;

namespace CardCollection.Core
{
    public interface ICardCollectionCompositionRoot
    {
        IWindowPresenter CreateWindowPresenter();
        IOfferRewardsReceiver CreateOfferRewardsReceiver();
        ICardCollectionRewardHandler CreateRewardHandler(IOfferRewardsReceiver offerRewardsReceiver, IRewardDefinitionFactory rewardDefinitionFactory);
        CardCollectionModuleConfig CreateModuleConfig(ICardPackProvider cardPackProvider, string eventId);
        IRewardDefinitionFactory CreateRewardDefinitionFactory(List<CardPackConfig> cardPackConfigs);
        IExchangeOfferProvider CreateExchangeOfferProvider(ICardCollectionRewardHandler rewardHandler);
    }
}