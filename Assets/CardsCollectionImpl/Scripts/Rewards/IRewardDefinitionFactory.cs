using CardCollection.Core;

namespace CardCollectionImpl
{
    public interface IRewardDefinitionFactory
    {
        CollectionRewardDefinition CreateFromGroupReward(CollectionCompletionRewardConfig collectionCompletionRewardConfig);
        CollectionRewardDefinition CreateFromCollectionReward(FullCollectionRewardConfig fullCollectionRewardConfig = default);
        CollectionRewardDefinition CreateFromOfferReward(string offerPackId);
    }
}
