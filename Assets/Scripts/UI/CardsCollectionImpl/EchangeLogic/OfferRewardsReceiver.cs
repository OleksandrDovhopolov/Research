using System.Threading;
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

            if (_resourceManager == null)
            {
                return UniTask.FromResult(false);
            }

            switch (offerContent)
            {
                case BaseOfferContent baseOfferContent:
                    TryGetOfferReward(baseOfferContent, ct); 
                    break;
                case CardCollectionRewardContent  cardCollectionRewardContent:
                    TryGetOfferReward(cardCollectionRewardContent, ct);
                    break;
                }

            return UniTask.FromResult(true);
        }

        private void TryGetOfferReward(BaseOfferContent baseOfferContent, CancellationToken ct = default)
        {
            if (baseOfferContent.Resources != null)
            {
                foreach (var rewardResource in baseOfferContent.Resources)
                {
                    ct.ThrowIfCancellationRequested();
                    if (rewardResource is not { Amount: > 0 })
                    {
                        continue;
                    }

                    _resourceManager.Add(rewardResource.Type, rewardResource.Amount);
                }
            }
        }

        private void TryGetOfferReward(CardCollectionRewardContent rewardContent, CancellationToken ct = default)
        {
            if (rewardContent.Resources != null)
            {
                foreach (var rewardResource in rewardContent.Resources)
                {
                    ct.ThrowIfCancellationRequested();
                    if (rewardResource is not { Amount: > 0 })
                    {
                        continue;
                    }

                    _resourceManager.Add(rewardResource.Type, rewardResource.Amount);
                }
            }
        }
    }
}
