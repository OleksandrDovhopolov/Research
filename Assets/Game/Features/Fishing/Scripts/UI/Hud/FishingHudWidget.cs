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

namespace Game.Fishing
{
    public sealed class FishingHudWidget : MonoBehaviour, IHudWidget, IHudWidgetLifecycle
    {
        private const int SpeedUpCost = 50;
        private const string SpeedUpReason = "fishing_lure_speed_up";
        private const string SpeedUpRefundReason = "fishing_lure_speed_up_refund";

        [SerializeField] private Canvas _canvas;
        [SerializeField] private UIListPool<LureView> _lurePool;
        [SerializeField] private DropUITarget _dropTarget;
        [SerializeField] private Button _speedUpButton;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private GameObject _idleStateObject;
        [SerializeField] private GameObject _productionStateObject;
        [SerializeField] private GameObject _timerStateObject;

        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private readonly Dictionary<string, Task<Sprite>> _spriteLoadTasks = new();
        private readonly Dictionary<LureView, FishingHudLureViewData> _luresByView = new();
        private readonly HashSet<string> _requestedSpriteAddresses = new();

        private ICraftingService _craftingService;
        private ResourceManager _resourceManager;
        private IResourceOperationsService _resourceOperationsService;
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
            IResourceOperationsService resourceOperationsService)
        {
            UnsubscribeFromResources();

            _craftingService = craftingService;
            _resourceManager = resourceManager;
            _resourceOperationsService = resourceOperationsService;

            SubscribeToResources();
            UpdateSpeedUpButtonState();
        }

        private void Awake()
        {
            _canvas ??= GetComponent<Canvas>();
            SetText(_priceText, SpeedUpCost.ToString());

            if (_speedUpButton != null)
                _speedUpButton.onClick.AddListener(OnSpeedUpClicked);

            ApplyCraftState();
            UpdateSpeedUpButtonState();
        }

        /*private void LateUpdate()
        {
            HudCameraFacingUtility.FaceCamera(transform, _canvas);
        }*/

        public void OnCreatedByHudController()
        {
            ApplyCraftState();
            UpdateSpeedUpButtonState();
        }

        public void OnBeforeReleased()
        {
            Dispose();
        }

        public async UniTask RenderAsync(IReadOnlyList<FishingHudLureViewData> lures, CancellationToken ct)
        {
            HudCameraFacingUtility.FaceCamera(transform, _canvas);
            
            ct.ThrowIfCancellationRequested();
            var renderVersion = ++_renderVersion;
            _luresByView.Clear();
            _lurePool?.DisableAll();

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
                    onBeginDrag: null,
                    onDrag: null,
                    onEndDrag: eventData => OnLureDroppedAsync(lure, eventData).Forget());
                view.SetDragLocked(ShouldLockLure(lure));

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

        private async UniTaskVoid OnLureDroppedAsync(FishingHudLureViewData lure, PointerEventData eventData)
        {
            if (_isDisposed || lure == null || _dropTarget == null || eventData == null)
                return;

            if (!_dropTarget.IsPositionInsideRect(eventData.position))
                return;

            if (_hasActiveCraft || _isCraftOperationRunning)
                return;

            if (_craftingService == null)
            {
                Debug.LogWarning("[FishingHudWidget] Crafting service is not assigned.");
                return;
            }

            if (lure.Count <= 0 || string.IsNullOrWhiteSpace(lure.CraftRecipeId))
                return;

            _isCraftOperationRunning = true;
            UpdateLureDragLocks();
            UpdateSpeedUpButtonState();

            try
            {
                var start = await _craftingService.StartCraftAsync(
                    lure.CraftRecipeId,
                    this.GetCancellationTokenOnDestroy());

                if (!start.Success)
                {
                    if (start.Error != CraftingError.StationQueueFull)
                        Debug.LogWarning($"[FishingHudWidget] Failed to start lure craft '{lure.CraftRecipeId}'. Error={start.Error}.");

                    return;
                }

                SetActiveCraft(start.TaskId, start.CompleteAtUtc);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingHudWidget] Failed to start lure craft '{lure.CraftRecipeId}'. {exception}");
            }
            finally
            {
                _isCraftOperationRunning = false;
                UpdateLureDragLocks();
                UpdateSpeedUpButtonState();
            }
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
                lureView.SetDragLocked(shouldLock || ShouldLockLure(lure));
            }
        }

        private bool ShouldLockLure(FishingHudLureViewData lure)
        {
            return _hasActiveCraft ||
                   _isCraftOperationRunning ||
                   lure == null ||
                   lure.Count <= 0 ||
                   string.IsNullOrWhiteSpace(lure.CraftRecipeId);
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
            StopTimer();
            UnsubscribeFromResources();

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
