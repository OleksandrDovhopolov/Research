using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIShared
{
    public partial class ContentWidgetView
    {
        private int CreateAndGetOfferViews(ContentWidgetData contentData)
        {
            _itemsPool.DisableAll();

            if (contentData == null)
            {
                return 0;
            }

            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            var packRequests = CreateCardPacksViews(contentData);
            var resourceRequests = CreateResourcesViews(contentData);
            LoadContentSpritesSequentially(packRequests, resourceRequests, _loadSpritesCts.Token).Forget();

            return _itemsPool.ActiveElements().Count();
        }
        

        private SpriteLoadRequest[] CreateCardPacksViews(ContentWidgetData contentData)
        {
            if (contentData?.CardPackAddresses == null || contentData.CardPackAddresses.Count == 0)
            {
                return Array.Empty<SpriteLoadRequest>();
            }

            var requests = new SpriteLoadRequest[contentData.CardPackAddresses.Count];
            for (var i = 0; i < contentData.CardPackAddresses.Count; i++)
            {
                var cardPackAddress = contentData.CardPackAddresses[i];
                var contentView = _itemsPool.GetNext();
                contentView.SetText("x1");
                contentView.SetLoadingActive(true);
                requests[i] = new SpriteLoadRequest(contentView, cardPackAddress);
            }

            return requests;
        }

        private SpriteLoadRequest[] CreateResourcesViews(ContentWidgetData contentData)
        {
            if (contentData?.Resources == null || contentData.Resources.Count == 0)
            {
                return Array.Empty<SpriteLoadRequest>();
            }

            var requests = new SpriteLoadRequest[contentData.Resources.Count];
            for (var i = 0; i < contentData.Resources.Count; i++)
            {
                var contentResource = contentData.Resources[i];
                var contentView = _itemsPool.GetNext();
                contentView.SetText($"x{Mathf.Max(1, contentResource.Amount)}");
                contentView.SetLoadingActive(true);
                requests[i] = new SpriteLoadRequest(contentView, contentResource.Address);
            }

            return requests;
        }
    }
}