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

            if (offerContent is not BaseOfferContent baseOfferContent)
            {
                return UniTask.FromResult(false);
            }

            if (baseOfferContent.Resources != null)
            {
                foreach (var rewardResource in baseOfferContent.Resources)
                {
                    ct.ThrowIfCancellationRequested();
                    if (rewardResource == null || rewardResource.Amount <= 0)
                    {
                        continue;
                    }

                    _resourceManager.Add(rewardResource.Type, rewardResource.Amount);
                }
            }

            return UniTask.FromResult(true);
        }
    }
}
