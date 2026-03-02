using System.Threading;
using CardCollection.Core;
using CardCollectionImpl;

namespace core
{
    public interface IRewardDefinitionFactory
    {
        CollectionRewardDefinition CreateFromGroupReward(CollectionCompletionRewardConfig collectionCompletionRewardConfig);
        CollectionRewardDefinition CreateFromCollectionReward(FullCollectionRewardConfig fullCollectionRewardConfig = default);
    }

    public interface IOfferDefinitionFactory
    {
        CollectionRewardDefinition CreateFromOfferReward(string offerPackId, CancellationToken ct = default);
    }
}
