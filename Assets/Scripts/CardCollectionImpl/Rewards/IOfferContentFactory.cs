using System.Collections.Generic;
using CardCollection.Core;

namespace core
{
    public interface IOfferContentFactory
    {
        CardCollectionImpl.CollectionRewardDefinition CreateFromGroupReward(GroupRewardDefinition groupRewardDefinition);
        CardCollectionImpl.CollectionRewardDefinition CreateFromCollectionReward(CollectionRewardDefinition collectionRewardDefinition);
        CardCollectionImpl.CollectionRewardDefinition CreateFromExchangePack(ExchangePackEntry exchangePackEntry, IReadOnlyCollection<CardPack> cardPacks);
    }
}
