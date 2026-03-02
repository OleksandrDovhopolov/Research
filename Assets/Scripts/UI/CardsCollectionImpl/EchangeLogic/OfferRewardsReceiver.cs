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

        public UniTask<bool> ReceiveRewardsAsync(OfferContent offerContent, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_resourceManager == null || offerContent == null)
            {
                return UniTask.FromResult(false);
            }

            TryGetResourceRewards(offerContent, ct);

            return UniTask.FromResult(true);
        }

        private void TryGetResourceRewards(OfferContent offerContent, CancellationToken ct = default)
        {
            var resources = GetResources(offerContent);
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

        private static System.Collections.Generic.IReadOnlyCollection<GameResource> GetResources(OfferContent offerContent)
        {
            return offerContent switch
            {
                BaseOfferContent baseOfferContent => baseOfferContent.Resources,
                CardCollectionRewardContent collectionRewardContent => collectionRewardContent.Resources,
                CardGroupCompletedContent groupCompletedContent => groupCompletedContent.Resources,
                _ => null
            };
        }
    }
}
