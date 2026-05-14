using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Fishing
{
    public abstract class LureListHudWidgetBase : MonoBehaviour, IHudWidget, IHudWidgetLifecycle, IRectMissTap
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private UIListPool<LureView> _lurePool;
        [SerializeField] private DropUITarget _dropTarget;

        [Space, Space, Header("Drag")]
        [SerializeField] private LureDragPreviewView _dragPreviewView;
        [SerializeField] private RectTransform _dragPreviewActiveContainer;
        [SerializeField] private RectTransform _dragPreviewInactiveContainer;

        [Space, Space, Header("General")]
        [SerializeField] private RectTransform[] _missTapRects;

        private readonly Dictionary<LureView, FishingHudLureViewData> _luresByView = new();

        private HudMissTapInputController _missTapInputController;
        private RectTransform _rootRectTransform;
        private bool _isDisposed;

        protected bool IsDisposed => _isDisposed;
        protected Canvas Canvas => _canvas;

        protected void InstallBase(HudMissTapInputController missTapInputController)
        {
            _missTapInputController = missTapInputController;
            RegisterMissTap();
            OnInstalled();
        }

        protected virtual void Awake()
        {
            _canvas ??= GetComponent<Canvas>();
            _rootRectTransform = transform as RectTransform;
        }

        protected virtual void OnEnable()
        {
            RegisterMissTap();
        }

        protected virtual void OnDisable()
        {
            HideDragPreview();
            UnregisterMissTap();
        }

        public virtual void OnCreatedByHudController()
        {
            HideDragPreview();
            OnCreatedByHudControllerInternal();
        }

        public void OnBeforeReleased()
        {
            Dispose();
        }

        public bool OnMissTap()
        {
            HideDragPreview();
            HideHud();
            return true;
        }

        public IEnumerable<RectTransform> GetRectTransform()
        {
            if (_missTapRects is { Length: > 0 })
                return _missTapRects;

            _rootRectTransform ??= transform as RectTransform;
            return _rootRectTransform != null
                ? new[] { _rootRectTransform }
                : Array.Empty<RectTransform>();
        }

        protected void RenderLureViews(IReadOnlyList<FishingHudLureRenderData> lures, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            HideDragPreview();
            _luresByView.Clear();
            _lurePool?.DisableAll();
            OnBeforeRenderLures();

            var safeLures = lures ?? Array.Empty<FishingHudLureRenderData>();
            if (_lurePool == null || safeLures.Count == 0)
                return;

            for (var i = 0; i < safeLures.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var renderData = safeLures[i];
                var lure = renderData?.Lure;
                if (lure == null)
                    continue;

                var view = _lurePool.GetNext();
                _luresByView[view] = lure;
                view.transform.SetSiblingIndex(i);
                view.SetData(renderData.Sprite, lure.Count);
                view.SetDragHandlers(
                    onBeginDrag: eventData => OnLureBeginDrag(lure, view, eventData),
                    onLockedBeginDrag: eventData => OnLureLockedBeginDrag(lure),
                    onDrag: OnLureDrag,
                    onEndDrag: eventData => OnLureEndDrag(lure, eventData));
                view.SetDragLocked(ShouldLockLure(lure));
                OnLureViewConfigured(renderData, view);
            }
        }

        protected void RefreshDragLocks()
        {
            if (_lurePool == null)
                return;

            foreach (var lureView in _lurePool.ActiveElements())
            {
                _luresByView.TryGetValue(lureView, out var lure);
                lureView.SetDragLocked(ShouldLockLure(lure));
            }
        }

        protected virtual void OnInstalled()
        {
        }

        protected virtual void OnCreatedByHudControllerInternal()
        {
        }

        protected virtual void OnBeforeRenderLures()
        {
        }

        protected virtual void OnLureViewConfigured(FishingHudLureRenderData renderData, LureView view)
        {
        }

        protected abstract bool ShouldLockLure(FishingHudLureViewData lure);
        protected abstract string GetLureDragBlockedMessage(FishingHudLureViewData lure);
        protected abstract UniTask HandleDroppedLureAsync(FishingHudLureViewData lure, CancellationToken ct);
        protected abstract void ShowInfo(string message);
        protected abstract void HideHud();
        protected virtual void OnDisposing()
        {
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        private void OnLureBeginDrag(FishingHudLureViewData lure, LureView view, PointerEventData eventData)
        {
            if (_isDisposed || lure == null || view == null || eventData == null)
                return;

            if (ShouldLockLure(lure))
                return;

            if (_dragPreviewActiveContainer != null && _dragPreviewView != null)
                _dragPreviewView.transform.SetParent(_dragPreviewActiveContainer, false);

            _dragPreviewView?.Show(view.CurrentSprite, view.CurrentCount);
            _dragPreviewView?.MoveToScreenPosition(eventData.position);
        }

        private void OnLureLockedBeginDrag(FishingHudLureViewData lure)
        {
            var message = GetLureDragBlockedMessage(lure);
            if (!string.IsNullOrWhiteSpace(message))
                ShowInfo(message);
        }

        private void OnLureDrag(PointerEventData eventData)
        {
            if (_isDisposed || eventData == null || _dragPreviewView == null)
                return;

            _dragPreviewView.MoveToScreenPosition(eventData.position);
        }

        private void OnLureEndDrag(FishingHudLureViewData lure, PointerEventData eventData)
        {
            var isDroppedInsideTarget = !_isDisposed &&
                                        lure != null &&
                                        _dropTarget != null &&
                                        eventData != null &&
                                        _dropTarget.IsPositionInsideRect(eventData.position);

            HideDragPreview();

            if (isDroppedInsideTarget)
                HandleDroppedLureAsync(lure, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void HideDragPreview()
        {
            if (_dragPreviewView == null)
                return;

            if (_dragPreviewInactiveContainer != null)
                _dragPreviewView.transform.SetParent(_dragPreviewInactiveContainer, false);

            _dragPreviewView.Hide();
        }

        private void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            OnDisposing();
            UnregisterMissTap();
            HideDragPreview();
            _lurePool?.DisableAll();
            _luresByView.Clear();
        }

        private void RegisterMissTap()
        {
            if (isActiveAndEnabled)
                _missTapInputController?.AddHud(this);
        }

        private void UnregisterMissTap()
        {
            _missTapInputController?.RemoveHud(this);
        }
    }
}
