using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public partial class ContentWidgetView
    {
        private int CreateAndGetOfferViews(CardCollectionRewardContent rewardContent)
        {
            Debug.LogWarning($"Debug CardCollectionRewardContent");
            _itemsPool.DisableAll();

            if (rewardContent == null)
            {
                return 0;
            }

            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            var packRequests = CreateCardPacksViews(null);
            var resourceRequests = CreateResourcesViews(rewardContent);
            LoadContentSpritesSequentially(packRequests, resourceRequests, _loadSpritesCts.Token).Forget();

            return _itemsPool.ActiveElements().Count();
        }
        
        private SpriteLoadRequest[] CreateResourcesViews(CardCollectionRewardContent rewardContent)
        {
            if (rewardContent?.Resources == null || rewardContent.Resources.Count == 0)
            {
                return Array.Empty<SpriteLoadRequest>();
            }

            var requests = new SpriteLoadRequest[rewardContent.Resources.Count];
            for (var i = 0; i < rewardContent.Resources.Count; i++)
            {
                var contentResource = rewardContent.Resources[i];
                var contentView = _itemsPool.GetNext();
                contentView.SetText($"x{Mathf.Max(1, contentResource.Amount)}");
                contentView.SetLoadingActive(true);
                requests[i] = new SpriteLoadRequest(contentView, contentResource.Type.ToString());
            }

            return requests;
        }
    }
}