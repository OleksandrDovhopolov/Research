using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public partial class ContentWidgetView
    {
        private int CreateAndGetOfferViews(BaseOfferContent offerContent)
        {
            Debug.LogWarning($"Debug BaseOfferContent");
            _itemsPool.DisableAll();

            if (offerContent == null)
            {
                return 0;
            }

            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            var packRequests = CreateCardPacksViews(offerContent);
            var resourceRequests = CreateResourcesViews(offerContent);
            LoadContentSpritesSequentially(packRequests, resourceRequests, _loadSpritesCts.Token).Forget();

            return _itemsPool.ActiveElements().Count();
        }
        

        private SpriteLoadRequest[] CreateCardPacksViews(BaseOfferContent offerContent)
        {
            if (offerContent?.CardPack == null || offerContent.CardPack.Count == 0)
            {
                return Array.Empty<SpriteLoadRequest>();
            }

            var requests = new SpriteLoadRequest[offerContent.CardPack.Count];
            for (var i = 0; i < offerContent.CardPack.Count; i++)
            {
                var cardPack = offerContent.CardPack[i];
                var contentView = _itemsPool.GetNext();
                contentView.SetText("x1");
                contentView.SetLoadingActive(true);
                requests[i] = new SpriteLoadRequest(contentView, cardPack.PackId);
            }

            return requests;
        }

        private SpriteLoadRequest[] CreateResourcesViews(BaseOfferContent offerContent)
        {
            if (offerContent?.Resources == null || offerContent.Resources.Count == 0)
            {
                return Array.Empty<SpriteLoadRequest>();
            }

            var requests = new SpriteLoadRequest[offerContent.Resources.Count];
            for (var i = 0; i < offerContent.Resources.Count; i++)
            {
                var contentResource = offerContent.Resources[i];
                var contentView = _itemsPool.GetNext();
                contentView.SetText($"x{Mathf.Max(1, contentResource.Amount)}");
                contentView.SetLoadingActive(true);
                requests[i] = new SpriteLoadRequest(contentView, contentResource.Type.ToString());
            }

            return requests;
        }
    }
}