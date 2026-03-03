using System.Threading;

namespace CardCollection.Core
{
    public interface IRewardDefinitionFactory
    {
        CollectionRewardDefinition CreateFromGroupReward(CollectionCompletionRewardConfig collectionCompletionRewardConfig);
        CollectionRewardDefinition CreateFromCollectionReward(FullCollectionRewardConfig fullCollectionRewardConfig = default);
        CollectionRewardDefinition CreateFromOfferReward(string offerPackId, CancellationToken ct = default);
    }
}
