using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
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
        [SerializeField] private Image _productionIcon;
        [SerializeField] private RectTransform[] _missTapRects;

        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private readonly Dictionary<string, Task<Sprite>> _spriteLoadTasks = new();
        private readonly Dictionary<LureView, FishingHudLureViewData> _luresByView = new();
        private readonly HashSet<string> _requestedSpriteAddresses = new();

        private ICraftingService _craftingService;
        private UIManager _uiManager;
        private IClock _clock;
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
            UIManager uiManager,
            IClock clock,
            IHudController hudController,
            HudMissTapInputController missTapInputController)
        {
            _craftingService = craftingService;
            _uiManager = uiManager;
            _clock = clock;
            _hudController = hudController;
            _missTapInputController = missTapInputController;

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
            ct.ThrowIfCancellationRequested();
            var renderVersion = ++_renderVersion;
            _luresByView.Clear();
            _lurePool?.DisableAll();
            HideDragPreview();

            var safeLures = lures ?? Array.Empty<FishingHudLureViewData>();
            if (_lurePool == null || safeLures.Count == 0)
            {
                await RestoreActiveCraftAsync(safeLures, ct);
                return;
            }

            for (var i = 0; i < safeLures.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var lure = safeLures[i];
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

                await LoadLureSpriteAsync(view, lure, renderVersion, ct);
            }

            await RestoreActiveCraftAsync(safeLures, ct);
        }

        private async UniTask LoadLureSpriteAsync(
            LureView view,
            FishingHudLureViewData lure,
            int renderVersion,
            CancellationToken ct)
        {
            if (view == null || lure == null || string.IsNullOrWhiteSpace(lure.SpriteAddress))
                return;

            var sprite = await LoadSpriteAsync(lure, ct);
            if (renderVersion == _renderVersion && view != null)
                view.SetSprite(sprite);
        }

        private async UniTask<Sprite> LoadSpriteAsync(FishingHudLureViewData lure, CancellationToken ct)
        {
            if (lure == null || string.IsNullOrWhiteSpace(lure.SpriteAddress))
                return null;

            var spriteAddress = lure.SpriteAddress;
            if (_spriteCache.TryGetValue(spriteAddress, out var cachedSprite))
                return cachedSprite;

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
                    return null;
                }

                _spriteCache[spriteAddress] = sprite;
                return sprite;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FishingHudWidget] Failed to load sprite for lure '{lure.LureId}' at address '{spriteAddress}'. {exception}");
                return null;
            }
            finally
            {
                _spriteLoadTasks.Remove(spriteAddress);
            }
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

        private void OnLureLockedBeginDrag(FishingHudLureViewData lure, PointerEventData eventData)
        {
            var message = GetLureDragBlockedMessage(lure);
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_uiManager == null)
                return;

            _uiManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        private void OnLureDrag(PointerEventData eventData)
        {
            if (_isDisposed || eventData == null)
                return;

            if (_dragPreviewView == null)
                return;

            _dragPreviewView?.MoveToScreenPosition(eventData.position);
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
                TryStartLureProductionAsync(lure, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid TryStartLureProductionAsync(FishingHudLureViewData lure, CancellationToken ct)
        {
            if (lure == null || string.IsNullOrWhiteSpace(lure.CraftRecipeId))
            {
                Debug.LogWarning($"[FishingHudWidget] Craft start ignored. LureId='{lure?.LureId ?? "null"}', Recipe='{lure?.CraftRecipeId ?? "null"}'.");
                return;
            }

            if (_craftingService == null)
            {
                ShowInfo("Crafting service is not available.");
                Debug.LogWarning($"[FishingHudWidget] Craft start failed for lure '{lure.LureId}'. Crafting service is not assigned.");
                return;
            }

            _isCraftOperationRunning = true;
            UpdateLureDragLocks();
            UpdateSpeedUpButtonState();

            try
            {
                Debug.LogWarning($"[FishingHudWidget] Starting lure craft. LureId='{lure.LureId}', Recipe='{lure.CraftRecipeId}'.");
                var start = await _craftingService.StartCraftAsync(lure.CraftRecipeId, ct);
                if (!start.Success)
                {
                    ShowInfo(GetCraftingErrorMessage(start.Error));
                    Debug.LogWarning($"[FishingHudWidget] Craft start rejected. LureId='{lure.LureId}', Recipe='{lure.CraftRecipeId}', Error={start.Error}.");
                    return;
                }

                SetActiveCraft(start.TaskId, start.CompleteAtUtc);
                await SetProductionIconAsync(lure, ct);
                Debug.LogWarning($"[FishingHudWidget] Craft started. LureId='{lure.LureId}', Recipe='{lure.CraftRecipeId}', TaskId='{start.TaskId}', CompleteAtUtc='{start.CompleteAtUtc:O}'.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                ShowInfo("Failed to start lure production.");
                Debug.LogError($"[FishingHudWidget] Craft start crashed. LureId='{lure.LureId}', Recipe='{lure.CraftRecipeId}'. {exception}");
            }
            finally
            {
                _isCraftOperationRunning = false;
                UpdateLureDragLocks();
                UpdateSpeedUpButtonState();
            }
        }

        private void HideDragPreview()
        {
            if (_dragPreviewView == null)
                return;

            if (_dragPreviewInactiveContainer != null)
                _dragPreviewView.transform.SetParent(_dragPreviewInactiveContainer, false);

            _dragPreviewView.Hide();
        }

        private async UniTask RestoreActiveCraftAsync(IReadOnlyList<FishingHudLureViewData> lures, CancellationToken ct)
        {
            if (_craftingService == null)
                return;

            try
            {
                var activeTask = await _craftingService.GetFirstActiveTaskAsync(CraftingStationIds.LureCrafting, ct);
                if (activeTask == null)
                {
                    ClearActiveCraft();
                    return;
                }

                Debug.LogWarning($"[FishingHudWidget] Restoring active craft. TaskId='{activeTask.Id}', Recipe='{activeTask.Recipe?.Id}', CompleteAtUtc='{activeTask.CompleteAtUtc:O}'.");
                SetActiveCraft(activeTask.Id, activeTask.CompleteAtUtc);
                await SetProductionIconAsync(activeTask.Recipe?.Id, lures, ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FishingHudWidget] Failed to restore active lure craft. {exception}");
            }
        }

        private async UniTask SetProductionIconAsync(FishingHudLureViewData lure, CancellationToken ct)
        {
            var sprite = await LoadSpriteAsync(lure, ct);
            SetProductionIcon(sprite);
        }

        private async UniTask SetProductionIconAsync(
            string craftRecipeId,
            IReadOnlyList<FishingHudLureViewData> lures,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(craftRecipeId) || lures == null)
            {
                SetProductionIcon(null);
                return;
            }

            for (var i = 0; i < lures.Count; i++)
            {
                var lure = lures[i];
                if (lure == null || !string.Equals(lure.CraftRecipeId, craftRecipeId, StringComparison.Ordinal))
                    continue;

                await SetProductionIconAsync(lure, ct);
                return;
            }

            SetProductionIcon(null);
        }

        private void SetProductionIcon(Sprite sprite)
        {
            if (_productionIcon == null)
                return;

            _productionIcon.sprite = sprite;
            _productionIcon.enabled = sprite != null;
        }

        private void SetActiveCraft(CraftTaskId taskId, DateTimeOffset completeAtUtc)
        {
            _activeTaskId = taskId;
            _activeCompleteAtUtc = completeAtUtc;
            _hasActiveCraft = !taskId.IsEmpty;
            ApplyCraftState();
            StartTimer();
            UpdateSpeedUpButtonState();
        }

        private void ClearActiveCraft()
        {
            StopTimer();
            _activeTaskId = new CraftTaskId(string.Empty);
            _activeCompleteAtUtc = default;
            _hasActiveCraft = false;
            SetText(_timeText, string.Empty);
            SetProductionIcon(null);
            ApplyCraftState();
            UpdateSpeedUpButtonState();
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
                    var remaining = _activeCompleteAtUtc - GetCurrentTime();
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
                Debug.LogWarning($"[FishingHudWidget] Collecting lure craft. TaskId='{_activeTaskId}', RequireReady={requireReady}.");
                var collect = requireReady
                    ? await _craftingService.CollectAsync(_activeTaskId, ct)
                    : await _craftingService.CompleteAndCollectAsync(_activeTaskId, ct);

                if (!collect.Success)
                {
                    Debug.LogWarning($"[FishingHudWidget] Craft collect failed. TaskId='{_activeTaskId}', Error={collect.Error}.");
                    return;
                }

                Debug.LogWarning($"[FishingHudWidget] Craft collected. TaskId='{_activeTaskId}', OutputItemId='{collect.OutputItemId}', OutputCount={collect.OutputCount}.");
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
            if (!_hasActiveCraft || _isSpeedUpRunning)
                return;

            if (_craftingService == null)
            {
                Debug.LogWarning("[FishingHudWidget] Speed-up failed. Crafting service is not assigned.");
                return;
            }

            _isSpeedUpRunning = true;
            UpdateSpeedUpButtonState();

            try
            {
                Debug.LogWarning($"[FishingHudWidget] Speed-up requested for task '{_activeTaskId}'.");
                await CollectActiveCraftAsync(requireReady: false, ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingHudWidget] Speed-up crashed for task '{_activeTaskId}'. {exception}");
            }
            finally
            {
                _isSpeedUpRunning = false;
                UpdateLureDragLocks();
                UpdateSpeedUpButtonState();
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
            }
        }

        private bool ShouldLockLure(FishingHudLureViewData lure)
        {
            return _hasActiveCraft ||
                   _isCraftOperationRunning ||
                   lure == null ||
                   string.IsNullOrWhiteSpace(lure.CraftRecipeId);
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

        private string GetCraftingErrorMessage(CraftingError error)
        {
            return error switch
            {
                CraftingError.StationQueueFull => "Lure production is already running.",
                CraftingError.RecipeNotFound => "Craft recipe was not found.",
                CraftingError.RecipeDisabled => "This lure recipe is disabled.",
                CraftingError.TaskNotReady => "Lure production is not ready yet.",
                CraftingError.InventoryOperationFailed => "Failed to receive crafted lure.",
                CraftingError.ConfigInvalid => "Crafting configuration is invalid.",
                _ => "Failed to start lure production."
            };
        }

        private void ShowInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_uiManager == null)
            {
                Debug.LogWarning($"[FishingHudWidget] UIManager is not assigned. Info='{message}'.");
                return;
            }

            _uiManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
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

            _speedUpButton.gameObject.SetActive(_hasActiveCraft);
            _speedUpButton.interactable = _hasActiveCraft && !_isCraftOperationRunning && !_isSpeedUpRunning;
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

        private DateTimeOffset GetCurrentTime()
        {
            return _clock?.UtcNow ?? DateTimeOffset.UtcNow;
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
