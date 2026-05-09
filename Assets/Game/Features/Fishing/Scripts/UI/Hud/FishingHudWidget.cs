using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreResources;
using Cysharp.Threading.Tasks;
using Game.Crafting;
using Infrastructure;
using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using UISystem;

namespace Game.Fishing
{
    public sealed class FishingHudWidget : MonoBehaviour, IHudWidget, IHudWidgetLifecycle, IRectMissTap
    {
        private const int SpeedUpCost = 50;
        private const string SpeedUpReason = "fishing_lure_speed_up";
        private const string SpeedUpRefundReason = "fishing_lure_speed_up_refund";

        [SerializeField] private Canvas _canvas;
        [SerializeField] private UIListPool<LureView> _lurePool;
        [SerializeField] private DropUITarget _dropTarget;

        [Space, Space, Header("Drag")]
        [SerializeField] private LureDragPreviewView _dragPreviewView;
        [SerializeField] private RectTransform _dragPreviewActiveContainer;
        [SerializeField] private RectTransform _dragPreviewInactiveContainer;

        [Space, Space, Header("General")]
        [SerializeField] private Button _speedUpButton;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private GameObject _idleStateObject;
        [SerializeField] private GameObject _productionStateObject;
        [SerializeField] private GameObject _timerStateObject;
        [SerializeField] private RectTransform[] _missTapRects;

        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private readonly Dictionary<string, Task<Sprite>> _spriteLoadTasks = new();
        private readonly Dictionary<LureView, FishingHudLureViewData> _luresByView = new();
        private readonly HashSet<string> _requestedSpriteAddresses = new();

        private ICraftingService _craftingService;
        private ResourceManager _resourceManager;
        private IResourceOperationsService _resourceOperationsService;
        private UIManager _uiManager;
        private IHudController _hudController;
        private HudMissTapInputController _missTapInputController;
        private RectTransform _rootRectTransform;
        private CancellationTokenSource _timerCts;
        private CraftTaskId _activeTaskId;
        private DateTimeOffset _activeCompleteAtUtc;
        private int _renderVersion;
        private bool _hasActiveCraft;
        private bool _isCraftOperationRunning;
        private bool _isSpeedUpRunning;
        private bool _isDisposed;

        [Inject]
        public void Install(
            ICraftingService craftingService,
            ResourceManager resourceManager,
            IResourceOperationsService resourceOperationsService,
            UIManager uiManager,
            IHudController hudController,
            HudMissTapInputController missTapInputController)
        {
            UnsubscribeFromResources();

            _craftingService = craftingService;
            _resourceManager = resourceManager;
            _resourceOperationsService = resourceOperationsService;
            _uiManager = uiManager;
            _hudController = hudController;
            _missTapInputController = missTapInputController;

            SubscribeToResources();
            RegisterMissTap();
            UpdateSpeedUpButtonState();
        }

        private void Awake()
        {
            _canvas ??= GetComponent<Canvas>();
            _rootRectTransform = transform as RectTransform;
            SetText(_priceText, SpeedUpCost.ToString());

            if (_speedUpButton != null)
                _speedUpButton.onClick.AddListener(OnSpeedUpClicked);

            ApplyCraftState();
            UpdateSpeedUpButtonState();
        }

        private void OnEnable()
        {
            RegisterMissTap();
        }

        private void OnDisable()
        {
            HideDragPreview();
            UnregisterMissTap();
        }

        /*private void LateUpdate()
        {
            HudCameraFacingUtility.FaceCamera(transform, _canvas);
        }*/

        public void OnCreatedByHudController()
        {
            HideDragPreview();
            ApplyCraftState();
            UpdateSpeedUpButtonState();
        }

        public void OnBeforeReleased()
        {
            Dispose();
        }

        public bool OnMissTap()
        {
            HideDragPreview();

            if (_hudController != null)
            {
                _hudController.HideHudWidget<FishingHudWidget>();
                return true;
            }

            gameObject.SetActive(false);
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

        public async UniTask RenderAsync(IReadOnlyList<FishingHudLureViewData> lures, CancellationToken ct)
        {
            //HudCameraFacingUtility.FaceCamera(transform, _canvas);

            ct.ThrowIfCancellationRequested();
            var renderVersion = ++_renderVersion;
            _luresByView.Clear();
            _lurePool?.DisableAll();
            HideDragPreview();

            if (_lurePool == null || lures == null || lures.Count == 0)
                return;

            for (var i = 0; i < lures.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var lure = lures[i];
                if (lure == null)
                    continue;

                var view = _lurePool.GetNext();
                _luresByView[view] = lure;
                view.transform.SetSiblingIndex(i);
                view.SetData(null, lure.Count);
                view.SetDragHandlers(
                    onBeginDrag: eventData => OnLureBeginDrag(lure, view, eventData),
                    onLockedBeginDrag: eventData => OnLureLockedBeginDrag(lure, eventData),
                    onDrag: OnLureDrag,
                    onEndDrag: eventData => OnLureEndDrag(lure, eventData));
                var isDragLocked = ShouldLockLure(lure);
                view.SetDragLocked(isDragLocked);
                Debug.LogWarning($"[FishingHudWidget] Configure lure '{lure.LureId}'. DragLocked={isDragLocked}. {BuildLureLockDebugInfo(lure)}");

                await LoadLureSpriteAsync(view, lure, renderVersion, ct);
            }
        }

        private async UniTask LoadLureSpriteAsync(
            LureView view,
            FishingHudLureViewData lure,
            int renderVersion,
            CancellationToken ct)
        {
            if (view == null || lure == null || string.IsNullOrWhiteSpace(lure.SpriteAddress))
                return;

            var spriteAddress = lure.SpriteAddress;
            if (_spriteCache.TryGetValue(spriteAddress, out var cachedSprite))
            {
                if (renderVersion == _renderVersion && view != null)
                    view.SetSprite(cachedSprite);

                return;
            }

            try
            {
                ct.ThrowIfCancellationRequested();
                _requestedSpriteAddresses.Add(spriteAddress);

                if (!_spriteLoadTasks.TryGetValue(spriteAddress, out var loadTask))
                {
                    loadTask = ProdAddressablesWrapper.LoadAsync<Sprite>(spriteAddress, ct);
                    _spriteLoadTasks[spriteAddress] = loadTask;
                }

                var sprite = await loadTask.AsUniTask().AttachExternalCancellation(ct);
                if (sprite == null)
                {
                    Debug.LogWarning($"[FishingHudWidget] Sprite not found for lure '{lure.LureId}' at address '{spriteAddress}'.");
                    return;
                }

                _spriteCache[spriteAddress] = sprite;

                if (renderVersion == _renderVersion && view != null)
                    view.SetSprite(sprite);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FishingHudWidget] Failed to load sprite for lure '{lure.LureId}' at address '{spriteAddress}'. {exception}");
            }
            finally
            {
                _spriteLoadTasks.Remove(spriteAddress);
            }
        }

        private void OnLureBeginDrag(FishingHudLureViewData lure, LureView view, PointerEventData eventData)
        {
            if (_isDisposed || lure == null || view == null || eventData == null)
            {
                Debug.LogWarning($"[FishingHudWidget] Begin drag ignored. Disposed={_isDisposed}, LureNull={lure == null}, ViewNull={view == null}, EventNull={eventData == null}.");
                return;
            }

            if (ShouldLockLure(lure))
            {
                Debug.LogWarning($"[FishingHudWidget] Begin drag locked for lure '{lure.LureId}'. {BuildLureLockDebugInfo(lure)}");
                return;
            }

            if (_dragPreviewView == null)
                Debug.LogWarning($"[FishingHudWidget] Drag preview view is not assigned for lure '{lure.LureId}'.");

            if (_dragPreviewActiveContainer == null)
                Debug.LogWarning($"[FishingHudWidget] Drag preview active container is not assigned for lure '{lure.LureId}'.");

            if (_dragPreviewActiveContainer != null && _dragPreviewView != null)
                _dragPreviewView.transform.SetParent(_dragPreviewActiveContainer, false);

            Debug.LogWarning($"[FishingHudWidget] Begin drag for lure '{lure.LureId}'. SpriteAssigned={view.CurrentSprite != null}, Count={view.CurrentCount}.");
            _dragPreviewView?.Show(view.CurrentSprite, view.CurrentCount);
            _dragPreviewView?.MoveToScreenPosition(eventData.position);
        }

        private void OnLureLockedBeginDrag(FishingHudLureViewData lure, PointerEventData eventData)
        {
            var message = GetLureDragBlockedMessage(lure);
            Debug.LogWarning($"[FishingHudWidget] Locked drag attempt for lure '{lure?.LureId ?? "null"}'. Message='{message}'. EventNull={eventData == null}.");

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_uiManager == null)
            {
                Debug.LogWarning($"[FishingHudWidget] UIManager is not assigned. Cannot show info widget for locked lure '{lure?.LureId ?? "null"}'.");
                return;
            }

            _uiManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        private void OnLureDrag(PointerEventData eventData)
        {
            if (_isDisposed || eventData == null)
            {
                Debug.LogWarning($"[FishingHudWidget] Drag ignored. Disposed={_isDisposed}, EventNull={eventData == null}.");
                return;
            }

            if (_dragPreviewView == null)
            {
                Debug.LogWarning("[FishingHudWidget] Drag received, but drag preview view is not assigned.");
                return;
            }

            _dragPreviewView?.MoveToScreenPosition(eventData.position);
        }

        private void OnLureEndDrag(FishingHudLureViewData lure, PointerEventData eventData)
        {
            Debug.LogWarning($"[FishingHudWidget] End drag for lure '{lure?.LureId ?? "null"}'. DropTargetAssigned={_dropTarget != null}, EventNull={eventData == null}.");
            var isDroppedInsideTarget = !_isDisposed &&
                                        lure != null &&
                                        _dropTarget != null &&
                                        eventData != null &&
                                        _dropTarget.IsPositionInsideRect(eventData.position);

            Debug.LogWarning($"[FishingHudWidget] End drag result for lure '{lure?.LureId ?? "null"}'. DroppedInsideTarget={isDroppedInsideTarget}.");

            HideDragPreview();

            if (isDroppedInsideTarget)
                TryStartLureProduction(lure);
        }

        private void TryStartLureProduction(FishingHudLureViewData lure)
        {
            if (lure == null || string.IsNullOrWhiteSpace(lure.CraftRecipeId))
            {
                Debug.LogWarning($"[FishingHudWidget] Production start ignored. LureNull={lure == null}, Count={lure?.Count ?? 0}, Recipe='{lure?.CraftRecipeId ?? "null"}'.");
                return;
            }

            Debug.Log($"[FishingHudWidget] Lure '{lure.LureId}' started production. Recipe='{lure.CraftRecipeId}'.");
        }

        private void HideDragPreview()
        {
            if (_dragPreviewView == null)
            {
                Debug.LogWarning("[FishingHudWidget] HideDragPreview skipped because drag preview view is not assigned.");
                return;
            }

            if (_dragPreviewInactiveContainer != null)
                _dragPreviewView.transform.SetParent(_dragPreviewInactiveContainer, false);
            else
                Debug.LogWarning("[FishingHudWidget] Drag preview inactive container is not assigned.");

            _dragPreviewView.Hide();
        }

        private void SetActiveCraft(CraftTaskId taskId, DateTimeOffset completeAtUtc)
        {
            _activeTaskId = taskId;
            _activeCompleteAtUtc = completeAtUtc;
            _hasActiveCraft = !taskId.IsEmpty;
            ApplyCraftState();
            StartTimer();
        }

        private void ClearActiveCraft()
        {
            StopTimer();
            _activeTaskId = new CraftTaskId(string.Empty);
            _activeCompleteAtUtc = default;
            _hasActiveCraft = false;
            SetText(_timeText, string.Empty);
            ApplyCraftState();
        }

        private void StartTimer()
        {
            StopTimer();
            if (!_hasActiveCraft)
                return;

            _timerCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            UpdateTimerAsync(_timerCts.Token).Forget();
        }

        private void StopTimer()
        {
            if (_timerCts == null)
                return;

            _timerCts.Cancel();
            _timerCts.Dispose();
            _timerCts = null;
        }

        private async UniTaskVoid UpdateTimerAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _hasActiveCraft)
                {
                    var remaining = _activeCompleteAtUtc - DateTimeOffset.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                    {
                        SetText(_timeText, FormatTime(TimeSpan.Zero));
                        await CollectActiveCraftAsync(requireReady: true, ct);
                        return;
                    }

                    SetText(_timeText, FormatTime(remaining));
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask CollectActiveCraftAsync(bool requireReady, CancellationToken ct)
        {
            if (!_hasActiveCraft || _craftingService == null)
                return;

            _isCraftOperationRunning = true;
            UpdateLureDragLocks();
            UpdateSpeedUpButtonState();

            try
            {
                var collect = requireReady
                    ? await _craftingService.CollectAsync(_activeTaskId, ct)
                    : await _craftingService.CompleteAndCollectAsync(_activeTaskId, ct);

                if (!collect.Success)
                {
                    Debug.LogWarning($"[FishingHudWidget] Failed to collect lure craft '{_activeTaskId}'. Error={collect.Error}.");
                    return;
                }

                ClearActiveCraft();
            }
            finally
            {
                _isCraftOperationRunning = false;
                UpdateLureDragLocks();
                UpdateSpeedUpButtonState();
            }
        }

        private void OnSpeedUpClicked()
        {
            SpeedUpActiveCraftAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid SpeedUpActiveCraftAsync(CancellationToken ct)
        {
            if (!_hasActiveCraft || _isSpeedUpRunning || _isCraftOperationRunning)
                return;

            if (_craftingService == null || _resourceManager == null || _resourceOperationsService == null)
            {
                Debug.LogWarning("[FishingHudWidget] Crafting or resource services are not assigned.");
                return;
            }

            if (_resourceManager.Get(ResourceType.Gems) < SpeedUpCost)
                return;

            _isSpeedUpRunning = true;
            _isCraftOperationRunning = true;
            var charged = false;
            UpdateLureDragLocks();
            UpdateSpeedUpButtonState();

            try
            {
                charged = await _resourceOperationsService.RemoveAsync(
                    ResourceType.Gems,
                    SpeedUpCost,
                    SpeedUpReason,
                    ct);

                if (!charged)
                    return;

                var collect = await _craftingService.CompleteAndCollectAsync(_activeTaskId, ct);
                if (collect.Success)
                {
                    ClearActiveCraft();
                    return;
                }

                await RefundSpeedUpAsync(ct);
                Debug.LogError($"[FishingHudWidget] Speed-up collect failed for task '{_activeTaskId}'. Error={collect.Error}.");
            }
            catch (OperationCanceledException)
            {
                if (charged)
                    await RefundSpeedUpAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                if (charged)
                    await RefundSpeedUpAsync(CancellationToken.None);

                Debug.LogError($"[FishingHudWidget] Failed to speed up lure craft '{_activeTaskId}'. {exception}");
            }
            finally
            {
                _isSpeedUpRunning = false;
                _isCraftOperationRunning = false;
                UpdateLureDragLocks();
                UpdateSpeedUpButtonState();
            }
        }

        private async UniTask RefundSpeedUpAsync(CancellationToken ct)
        {
            if (_resourceOperationsService == null)
                return;

            try
            {
                await _resourceOperationsService.AddAsync(
                    ResourceType.Gems,
                    SpeedUpCost,
                    SpeedUpRefundReason,
                    ct);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingHudWidget] Failed to refund speed-up cost. {exception}");
            }
        }

        private void UpdateLureDragLocks()
        {
            if (_lurePool == null)
                return;

            var shouldLock = _hasActiveCraft || _isCraftOperationRunning;
            foreach (var lureView in _lurePool.ActiveElements())
            {
                _luresByView.TryGetValue(lureView, out var lure);
                var isLocked = shouldLock || ShouldLockLure(lure);
                lureView.SetDragLocked(isLocked);
                Debug.LogWarning($"[FishingHudWidget] Update lure drag lock for '{lure?.LureId ?? "null"}'. DragLocked={isLocked}. {BuildLureLockDebugInfo(lure)}");
            }
        }

        private bool ShouldLockLure(FishingHudLureViewData lure)
        {
            return _hasActiveCraft ||
                   _isCraftOperationRunning ||
                   lure == null ||
                   string.IsNullOrWhiteSpace(lure.CraftRecipeId);
        }

        private string BuildLureLockDebugInfo(FishingHudLureViewData lure)
        {
            return $"LureNull={lure == null}, Count={lure?.Count ?? 0}, Recipe='{lure?.CraftRecipeId ?? "null"}', HasActiveCraft={_hasActiveCraft}, IsCraftOperationRunning={_isCraftOperationRunning}.";
        }

        private string GetLureDragBlockedMessage(FishingHudLureViewData lure)
        {
            if (_hasActiveCraft || _isCraftOperationRunning)
                return "Lure production is already running.";

            if (lure == null)
                return "Lure is not configured.";

            if (string.IsNullOrWhiteSpace(lure.CraftRecipeId))
                return "This lure does not have a craft recipe configured.";

            return "Lure production cannot be started right now.";
        }

        private void ApplyCraftState()
        {
            SetActive(_idleStateObject, !_hasActiveCraft);
            SetActive(_productionStateObject, _hasActiveCraft);
            SetActive(_timerStateObject, _hasActiveCraft);
        }

        private void UpdateSpeedUpButtonState()
        {
            if (_speedUpButton == null)
                return;

            var hasEnoughGems = _resourceManager != null && _resourceManager.Get(ResourceType.Gems) >= SpeedUpCost;
            _speedUpButton.interactable = _hasActiveCraft && hasEnoughGems && !_isCraftOperationRunning && !_isSpeedUpRunning;
        }

        private void SubscribeToResources()
        {
            if (_resourceManager != null)
                _resourceManager.ResourceAmountChanged += OnResourceAmountChanged;
        }

        private void UnsubscribeFromResources()
        {
            if (_resourceManager != null)
                _resourceManager.ResourceAmountChanged -= OnResourceAmountChanged;
        }

        private void OnResourceAmountChanged(ResourceType type, int newAmount)
        {
            if (type == ResourceType.Gems)
                UpdateSpeedUpButtonState();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            UnregisterMissTap();
            StopTimer();
            UnsubscribeFromResources();
            HideDragPreview();

            if (_speedUpButton != null)
                _speedUpButton.onClick.RemoveListener(OnSpeedUpClicked);

            _lurePool?.DisableAll();
            _luresByView.Clear();

            foreach (var spriteAddress in _requestedSpriteAddresses)
                ProdAddressablesWrapper.Release(spriteAddress);

            _requestedSpriteAddresses.Clear();
            _spriteCache.Clear();
            _spriteLoadTasks.Clear();
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

        private static string FormatTime(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
                value = TimeSpan.Zero;

            return value.TotalHours >= 1d
                ? $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}"
                : $"{value.Minutes:00}:{value.Seconds:00}";
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
                label.text = value ?? string.Empty;
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target != null)
                target.SetActive(isActive);
        }
    }
}
