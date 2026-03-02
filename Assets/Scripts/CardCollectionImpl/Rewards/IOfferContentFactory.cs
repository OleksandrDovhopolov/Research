using System.Collections.Generic;
using CardCollection.Core;
using CardCollectionImpl;

namespace core
{
    public interface IOfferContentFactory
    {
        OfferContent CreateFromGroupReward(GroupRewardDefinition groupRewardDefinition);
        OfferContent CreateFromCollectionReward(CollectionRewardDefinition collectionRewardDefinition);
        OfferContent CreateFromExchangePack(ExchangePackEntry exchangePackEntry, IReadOnlyCollection<CardPack> cardPacks);
    }
}
