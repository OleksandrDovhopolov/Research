using System.Collections;
using System;
using System.Collections.Generic;
using UISystem;
using UnityEngine;

namespace UIShared
{
    public class ContentWidgetView : WindowView 
    {
        private const int ContentWidgetHideDelay = 10;
        
        [SerializeField] private RectTransform _container;
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private float _verticalOffset = 24f;
        
        private RectTransform _contentRectTransform;
        private readonly Dictionary<Type, MonoBehaviour> _cachedViews = new();
        private MonoBehaviour _activeView;
        
        public void ShowContentView(ContentWidgetDataBase contentData, RectTransform contentRectTransform)
        {
            if (contentData == null)
            {
                Debug.LogError("Failed to show content widget. Content data is null.");
                HideContentWidget();
                return;
            }

            _contentRectTransform = contentRectTransform;
            
            StopAllCoroutines();
            DeactivateActiveView();

            var dataType = contentData.GetType();
            var prefabPrototype = WidgetRegistry.GetPrefab(dataType);

            if (prefabPrototype == null)
            {
                Debug.LogError($"Prefab {dataType} not found");
                return;
            }

            var viewInstance = GetOrCreateViewInstance(dataType, prefabPrototype);
            if (viewInstance == null)
            {
                HideContentWidget();
                return;
            }

            if (!viewInstance.TryGetComponent<IContentWidgetView>(out var view))
            {
                Debug.LogError($"View instance for {dataType} does not implement {nameof(IContentWidgetView)}");
                HideContentWidget();
                return;
            }

            viewInstance.gameObject.SetActive(true);
            _activeView = viewInstance;

            if (!view.Setup(contentData))
            {
                Debug.LogWarning($"Setup failed for content widget type {dataType}");
                HideContentWidget();
                return;
            }

            RepositionAboveClickedTransform();
            StartCoroutine(ResizeAndRepositionCoroutine(view.OnViewCreated()));
            StartCoroutine(HidePopupCoroutine());
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
        
        private IEnumerator ResizeAndRepositionCoroutine(IEnumerator routine)
        {
            yield return routine;
            RepositionAboveClickedTransform();
            ClampToParentBounds();
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

        public void Dispose()
        {
            StopAllCoroutines();
            _activeView = null;

            foreach (var cachedView in _cachedViews.Values)
            {
                if (cachedView != null)
                {
                    Destroy(cachedView.gameObject);
                }
            }

            _cachedViews.Clear();
        }

        private MonoBehaviour GetOrCreateViewInstance(Type dataType, IContentWidgetView prefabPrototype)
        {
            if (_cachedViews.TryGetValue(dataType, out var cachedView) && cachedView != null)
            {
                return cachedView;
            }

            if (prefabPrototype is not MonoBehaviour prefabBehaviour)
            {
                Debug.LogError($"Registered prefab for {dataType} is not a MonoBehaviour.");
                return null;
            }

            var instance = Instantiate(prefabBehaviour, _contentContainer);
            instance.gameObject.SetActive(false);
            _cachedViews[dataType] = instance;
            return instance;
        }

        private void DeactivateActiveView()
        {
            if (_activeView == null)
            {
                return;
            }

            _activeView.gameObject.SetActive(false);
            _activeView = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
        }
    }
}