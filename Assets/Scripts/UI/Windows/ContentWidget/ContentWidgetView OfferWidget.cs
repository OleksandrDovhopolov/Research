using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public partial class ContentWidgetView
    {
        private int CreateAndGetOfferViews(DuplicatePointsChestOffer collectionRewardDefinition)
        {
            Debug.LogWarning($"Debug BaseOfferContent");
            _itemsPool.DisableAll();

            if (collectionRewardDefinition == null)
            {
                return 0;
            }

            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            var packRequests = CreateCardPacksViews(collectionRewardDefinition);
            var resourceRequests = CreateResourcesViews(collectionRewardDefinition);
            LoadContentSpritesSequentially(packRequests, resourceRequests, _loadSpritesCts.Token).Forget();

            return _itemsPool.ActiveElements().Count();
        }
        

        private SpriteLoadRequest[] CreateCardPacksViews(DuplicatePointsChestOffer collectionRewardDefinition)
        {
            if (collectionRewardDefinition?.CardPack == null || collectionRewardDefinition.CardPack.Count == 0)
            {
                return Array.Empty<SpriteLoadRequest>();
            }

            var requests = new SpriteLoadRequest[collectionRewardDefinition.CardPack.Count];
            for (var i = 0; i < collectionRewardDefinition.CardPack.Count; i++)
            {
                var cardPack = collectionRewardDefinition.CardPack[i];
                var contentView = _itemsPool.GetNext();
                contentView.SetText("x1");
                contentView.SetLoadingActive(true);
                requests[i] = new SpriteLoadRequest(contentView, cardPack.PackId);
            }

            return requests;
        }

        private SpriteLoadRequest[] CreateResourcesViews(DuplicatePointsChestOffer collectionRewardDefinition)
        {
            if (collectionRewardDefinition?.Resources == null || collectionRewardDefinition.Resources.Count == 0)
            {
                return Array.Empty<SpriteLoadRequest>();
            }

            var requests = new SpriteLoadRequest[collectionRewardDefinition.Resources.Count];
            for (var i = 0; i < collectionRewardDefinition.Resources.Count; i++)
            {
                var contentResource = collectionRewardDefinition.Resources[i];
                var contentView = _itemsPool.GetNext();
                contentView.SetText($"x{Mathf.Max(1, contentResource.Amount)}");
                contentView.SetLoadingActive(true);
                requests[i] = new SpriteLoadRequest(contentView, contentResource.Type.ToString());
            }

            return requests;
        }
    }
}