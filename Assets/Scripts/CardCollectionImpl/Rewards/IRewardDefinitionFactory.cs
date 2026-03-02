using System.Collections.Generic;
using CardCollection.Core;

namespace core
{
    public interface IRewardDefinitionFactory
    {
        CardCollectionImpl.CollectionRewardDefinition CreateFromGroupReward(GroupRewardDefinition groupRewardDefinition);
        CardCollectionImpl.CollectionRewardDefinition CreateFromCollectionReward(CollectionRewardDefinition collectionRewardDefinition = default);
        CardCollectionImpl.CollectionRewardDefinition CreateFromExchangePack(ExchangePackEntry exchangePackEntry, IReadOnlyCollection<CardPack> cardPacks);
    }
}
