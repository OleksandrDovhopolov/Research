using System;
using System.Collections;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UIShared;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class CardsOfferWidgetView : MonoBehaviour, IContentWidgetView
    {
        private readonly struct SpriteLoadRequest
        {
            public readonly ContentItemView View;
            public readonly string Address;

            public SpriteLoadRequest(ContentItemView view, string address)
            {
                View = view;
                Address = address;
            }
        }
        
        [SerializeField] private RectTransform _container;
        [SerializeField] private UIListPool<ContentItemView> _itemsPool;
        [SerializeField] private HorizontalLayoutGroup _itemsGroup;
        
        private ContentWidgetData _contentData;
        
        private CancellationTokenSource _loadSpritesCts;
        
        public bool Setup(ContentWidgetDataBase data)
        {
            _contentData = (ContentWidgetData)data;

            if (_contentData == null)
            {
                Debug.LogWarning($"Failed to create cards widget data for {nameof(ContentWidgetData)}");
                return false;
            }
            
            var viewItems = CreateAndGetOfferViews(_contentData);
            
            if (viewItems <= 0)
            {
                
                Debug.LogWarning($"Failed to open content widget {GetType()}. _itemsPool count == 0");
                //HideContentWidget();
                return false;
            }

            
            
            return true;
        }

        public IEnumerator OnViewCreated()
        {
            return ResizeToLayout();
        }
        
        #region Resize
        
        private IEnumerator ResizeToLayout()
        {
            yield return new WaitForEndOfFrame();

            var itemsRect = _itemsGroup != null ? _itemsGroup.transform as RectTransform : null;
            if (itemsRect == null)
            {
                yield return null;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsRect);
            Canvas.ForceUpdateCanvases();
        }

        #endregion
        
        #region ViewsLoad
        
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
        
        private async UniTask LoadContentSpritesSequentially(
            SpriteLoadRequest[] packRequests,
            SpriteLoadRequest[] resourceRequests,
            CancellationToken ct)
        {
            try
            {
                await LoadSprites(packRequests, ct);
                await LoadSprites(resourceRequests, ct);
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        private static async UniTask LoadSprites(SpriteLoadRequest[] requests, CancellationToken ct)
        {
            foreach (var request in requests)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(request.Address);
                    request.View.SetSprite(sprite);
                }
                finally
                {
                    request.View.SetLoadingActive(false);
                }
            }
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

        #endregion
    }
}