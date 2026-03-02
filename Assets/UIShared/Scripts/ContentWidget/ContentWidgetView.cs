using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public partial class ContentWidgetView : WindowView 
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

        private const int ContentWidgetHideDelay = 10;
        
        [SerializeField] private UIListPool<ContentItemView> _itemsPool;

        [SerializeField] private RectTransform _container;
        [SerializeField] private HorizontalLayoutGroup _itemsGroup;
        [SerializeField] private float _verticalOffset = 24f;
        
        [Space, Header("MockSprite")]
        [SerializeField] private Sprite _mockSprite;
        
        private RectTransform _contentRectTransform;
        private CancellationTokenSource _loadSpritesCts;
        
        public void ShowContentView(ContentWidgetData contentData, RectTransform contentRectTransform)
        {
            _contentRectTransform = contentRectTransform;
            
            StopAllCoroutines();

            var viewItems = CreateAndGetOfferViews(contentData ?? ContentWidgetData.Empty);
            
            if (viewItems <= 0)
            {
                
                Debug.LogWarning($"Failed to open content widget {GetType()}. _itemsPool count == 0");
                HideContentWidget();
                return;
            }

            StartCoroutine(ResizeAndRepositionCoroutine());
            StartCoroutine(HidePopupCoroutine());
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
        
        private void HideContentWidget()
        {
            InvokeCloseEvent();
        }

        private IEnumerator HidePopupCoroutine()
        {
            yield return new WaitForSeconds(ContentWidgetHideDelay);
            HideContentWidget();
        }
        
        private IEnumerator ResizeAndRepositionCoroutine()
        {
            yield return new WaitForEndOfFrame();

            Canvas.ForceUpdateCanvases();
            ResizeToLayout();
            RepositionAboveClickedTransform();
            ClampToParentBounds();
        }

        private void ResizeToLayout()
        {
            var itemsRect = _itemsGroup != null ? _itemsGroup.transform as RectTransform : null;
            if (itemsRect == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsRect);
            Canvas.ForceUpdateCanvases();

            var preferredWidth = LayoutUtility.GetPreferredWidth(itemsRect);
            if (preferredWidth <= 0f)
            {
                preferredWidth = itemsRect.rect.width;
            }

            if (preferredWidth > 0f)
            {
                _container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
            }
        }

        private void RepositionAboveClickedTransform()
        {
            if (_contentRectTransform == null)
            {
                return;
            }

            var targetRect = _container.parent as RectTransform;
            if (targetRect == null)
            {
                var worldTopCenterNoParent = _contentRectTransform.TransformPoint(
                    new Vector3(0f, _contentRectTransform.rect.yMax, 0f));
                _container.position = worldTopCenterNoParent + _contentRectTransform.up * _verticalOffset;
                return;
            }

            var worldTopCenter = _contentRectTransform.TransformPoint(
                new Vector3(0f, _contentRectTransform.rect.yMax, 0f));

            var sourceCanvas = _contentRectTransform.GetComponentInParent<Canvas>();
            var sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? sourceCanvas.worldCamera
                : null;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldTopCenter);

            var targetCanvas = _container.GetComponentInParent<Canvas>();
            var targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPoint, targetCamera, out var localPoint))
            {
                _container.anchoredPosition = localPoint + Vector2.up * _verticalOffset;
            }
            else
            {
                _container.position = worldTopCenter + _contentRectTransform.up * _verticalOffset;
            }
        }

        private void ClampToParentBounds()
        {
            var parentRect = _container.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            var parentBounds = parentRect.rect;
            var containerRect = _container.rect;
            var pivot = _container.pivot;
            var anchoredPos = _container.anchoredPosition;

            var minX = parentBounds.xMin + containerRect.width * pivot.x;
            var maxX = parentBounds.xMax - containerRect.width * (1f - pivot.x);
            var minY = parentBounds.yMin + containerRect.height * pivot.y;
            var maxY = parentBounds.yMax - containerRect.height * (1f - pivot.y);

            anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
            anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);
            _container.anchoredPosition = anchoredPos;
        }

        private void OnDestroy()
        {
            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = null;
        }
    }
}