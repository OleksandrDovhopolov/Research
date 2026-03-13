using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared
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
        
        [Space, Header("Prefabs")]
        [SerializeField] private GameObject _cardsViewPrefab;
        [SerializeField] private GameObject _inventoryViewPrefab;
        [SerializeField] private Button _inventoryButton;
        
        private RectTransform _contentRectTransform;
        private CancellationTokenSource _loadSpritesCts;

        public event Action InventoryButtonClicked;
        
        public void ShowContentView(ContentWidgetDataBase contentData, RectTransform contentRectTransform)
        {
            _contentRectTransform = contentRectTransform;
            
            StopAllCoroutines();

            switch (contentData)
            {
                case InventoryWidgetData inventoryWidgetData:
                    SetInventoryWidget(inventoryWidgetData);
                    break;
                case ContentWidgetData contentWidgetData:
                    SetContentWidget(contentWidgetData);
                    break;
            }
            
            /*var isInventoryWidget = contentData is InventoryWidgetData;
            SwitchWidgetMode(isInventoryWidget);
            ConfigureInventoryButton(isInventoryWidget);
            
            if (isInventoryWidget)
            {
                _itemsPool.DisableAll();
                _loadSpritesCts?.Cancel();
                _loadSpritesCts?.Dispose();
                _loadSpritesCts = null;
                StartCoroutine(HidePopupCoroutine());
                return;
            }
            
            var widgetData = contentData as ContentWidgetData ?? ContentWidgetData.Empty;
            var viewItems = CreateAndGetOfferViews(widgetData);
            
            if (viewItems <= 0)
            {
                
                Debug.LogWarning($"Failed to open content widget {GetType()}. _itemsPool count == 0");
                HideContentWidget();
                return;
            }

            StartCoroutine(ResizeAndRepositionCoroutine());
            StartCoroutine(HidePopupCoroutine());*/
        }

        private void SetInventoryWidget(InventoryWidgetData inventoryWidgetData)
        {
            SwitchWidgetMode(true);
            ConfigureInventoryButton(true);
            
            _itemsPool.DisableAll();
            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = null;
            
            RepositionAboveClickedTransform();
            StartCoroutine(HidePopupCoroutine());
        }

        private void SetContentWidget(ContentWidgetData contentWidgetData)
        {
            SwitchWidgetMode(false);
            ConfigureInventoryButton(false);
            
            var viewItems = CreateAndGetOfferViews(contentWidgetData ?? ContentWidgetData.Empty);
            
            if (viewItems <= 0)
            {
                
                Debug.LogWarning($"Failed to open content widget {GetType()}. _itemsPool count == 0");
                HideContentWidget();
                return;
            }

            StartCoroutine(ResizeAndRepositionCoroutine());
            StartCoroutine(HidePopupCoroutine());
        }
        
        private void SwitchWidgetMode(bool isInventoryWidget)
        {
            if (_cardsViewPrefab != null)
            {
                _cardsViewPrefab.SetActive(!isInventoryWidget);
            }

            if (_inventoryViewPrefab != null)
            {
                _inventoryViewPrefab.SetActive(isInventoryWidget);
            }
        }

        private void ConfigureInventoryButton(bool isInventoryWidget)
        {
            if (_inventoryButton == null)
            {
                return;
            }

            _inventoryButton.onClick.RemoveListener(OnInventoryButtonClickedHandler);
            if (isInventoryWidget)
            {
                _inventoryButton.onClick.AddListener(OnInventoryButtonClickedHandler);
            }
        }

        private void OnInventoryButtonClickedHandler()
        {
            InventoryButtonClicked?.Invoke();
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_inventoryButton != null)
            {
                _inventoryButton.onClick.RemoveListener(OnInventoryButtonClickedHandler);
            }
            _loadSpritesCts?.Cancel();
            _loadSpritesCts?.Dispose();
            _loadSpritesCts = null;
        }
    }
}