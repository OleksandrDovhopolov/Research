using System.Collections.Generic;
using CardCollection.Core;

namespace core
{
    public interface IRewardDefinitionFactory
    {
        CardCollectionImpl.CollectionRewardDefinition CreateFromGroupReward(CollectionCompletionRewardConfig collectionCompletionRewardConfig);
        CardCollectionImpl.CollectionRewardDefinition CreateFromCollectionReward(FullCollectionRewardConfig fullCollectionRewardConfig = default);
        CardCollectionImpl.CollectionRewardDefinition CreateFromExchangePack(ExchangePackEntry exchangePackEntry, IReadOnlyCollection<CardPack> cardPacks);
    }
}
