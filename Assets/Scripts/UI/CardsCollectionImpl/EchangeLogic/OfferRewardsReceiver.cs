using System.Threading;
using CardCollectionImpl;
using Cysharp.Threading.Tasks;

namespace core
{
    public class OfferRewardsReceiver : IOfferRewardsReceiver
    {
        private readonly ResourceManager _resourceManager;

        public OfferRewardsReceiver(ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public UniTask<bool> ReceiveRewardsAsync(CardCollectionImpl.CollectionRewardDefinition collectionRewardDefinition, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_resourceManager == null || collectionRewardDefinition == null)
            {
                return UniTask.FromResult(false);
            }

            TryGetResourceRewards(collectionRewardDefinition, ct);

            return UniTask.FromResult(true);
        }

        private void TryGetResourceRewards(CardCollectionImpl.CollectionRewardDefinition collectionRewardDefinition, CancellationToken ct = default)
        {
            var resources = GetResources(collectionRewardDefinition);
            if (resources == null)
            {
                return;
            }
            
            foreach (var rewardResource in resources)
            {
                ct.ThrowIfCancellationRequested();
                if (rewardResource is not { Amount: > 0 })
                {
                    continue;
                }

                _resourceManager.Add(rewardResource.Type, rewardResource.Amount);
            }
        }

        private static System.Collections.Generic.IReadOnlyCollection<GameResource> GetResources(CardCollectionImpl.CollectionRewardDefinition collectionRewardDefinition)
        {
            return collectionRewardDefinition switch
            {
                DuplicatePointsChestOffer baseOfferContent => baseOfferContent.Resources,
                FullCollectionReward collectionRewardContent => collectionRewardContent.Resources,
                CardGroupCompletionReward groupCompletedContent => groupCompletedContent.Resources,
                _ => null
            };
        }
    }
}
