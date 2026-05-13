using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Crafting;
using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Fishing
{
    public sealed class FishingHudWidget : LureListHudWidgetBase
    {
        private const int SpeedUpCost = 50;

        [SerializeField] private Button _speedUpButton;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private GameObject _idleStateObject;
        [SerializeField] private GameObject _productionStateObject;
        [SerializeField] private GameObject _timerStateObject;
        [SerializeField] private Image _productionIcon;

        private readonly Dictionary<string, Sprite> _spritesByCraftRecipeId = new(StringComparer.Ordinal);

        private IFishingHudActions _fishingHudActions;
        private CancellationTokenSource _timerCts;
        private CraftTaskId _activeTaskId;
        private DateTimeOffset _activeCompleteAtUtc;
        private bool _hasActiveCraft;
        private bool _isCraftOperationRunning;
        private bool _isSpeedUpRunning;

        [Inject]
        public void Install(IFishingHudActions fishingHudActions, HudMissTapInputController missTapInputController)
        {
            _fishingHudActions = fishingHudActions;
            InstallBase(missTapInputController);
        }

        protected override void Awake()
        {
            base.Awake();
            SetText(_priceText, SpeedUpCost.ToString());
            SetProductionIcon(null);

            if (_speedUpButton != null)
                _speedUpButton.onClick.AddListener(OnSpeedUpClicked);

            ApplyCraftState();
            UpdateSpeedUpButtonState();
        }

        protected override void OnInstalled()
        {
            UpdateSpeedUpButtonState();
        }

        protected override void OnCreatedByHudControllerInternal()
        {
            ApplyCraftState();
            UpdateSpeedUpButtonState();
        }

        public async UniTask RenderAsync(IReadOnlyList<FishingHudLureRenderData> lures, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            RenderLureViews(lures, ct);
            await RestoreActiveCraftAsync(ct);
        }

        protected override void OnBeforeRenderLures()
        {
            _spritesByCraftRecipeId.Clear();
        }

        protected override void OnLureViewConfigured(FishingHudLureRenderData renderData, LureView view)
        {
            var lure = renderData?.Lure;
            if (lure == null || string.IsNullOrWhiteSpace(lure.CraftRecipeId) || renderData.Sprite == null)
                return;

            _spritesByCraftRecipeId[lure.CraftRecipeId] = renderData.Sprite;
        }

        protected override bool ShouldLockLure(FishingHudLureViewData lure)
        {
            return _hasActiveCraft ||
                   _isCraftOperationRunning ||
                   lure == null ||
                   string.IsNullOrWhiteSpace(lure.CraftRecipeId);
        }

        protected override string GetLureDragBlockedMessage(FishingHudLureViewData lure)
        {
            if (_hasActiveCraft || _isCraftOperationRunning)
                return "Lure production is already running.";

            if (lure == null)
                return "Lure is not configured.";

            if (string.IsNullOrWhiteSpace(lure.CraftRecipeId))
                return "This lure does not have a craft recipe configured.";

            return "Lure production cannot be started right now.";
        }

        protected override async UniTask HandleDroppedLureAsync(FishingHudLureViewData lure, CancellationToken ct)
        {
            await TryStartLureProductionAsync(lure, ct);
        }

        protected override void ShowInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_fishingHudActions == null)
            {
                Debug.LogWarning($"[FishingHudWidget] Fishing HUD actions are not assigned. Info='{message}'.");
                return;
            }

            _fishingHudActions.ShowInfo(message);
        }

        protected override void HideHud()
        {
            if (_fishingHudActions != null)
            {
                _fishingHudActions.HideHud();
                return;
            }

            gameObject.SetActive(false);
        }

        protected override void OnDisposing()
        {
            StopTimer();

            if (_speedUpButton != null)
                _speedUpButton.onClick.RemoveListener(OnSpeedUpClicked);

            _spritesByCraftRecipeId.Clear();
        }

        private async UniTask TryStartLureProductionAsync(FishingHudLureViewData lure, CancellationToken ct)
        {
            if (lure == null || string.IsNullOrWhiteSpace(lure.CraftRecipeId))
            {
                Debug.LogWarning($"[FishingHudWidget] Craft start ignored. LureId='{lure?.LureId ?? "null"}', Recipe='{lure?.CraftRecipeId ?? "null"}'.");
                return;
            }

            if (_fishingHudActions == null)
            {
                ShowInfo("Crafting service is not available.");
                Debug.LogWarning($"[FishingHudWidget] Craft start failed for lure '{lure.LureId}'. Fishing HUD actions are not assigned.");
                return;
            }

            _isCraftOperationRunning = true;
            RefreshDragLocks();
            UpdateSpeedUpButtonState();

            try
            {
                Debug.LogWarning($"[FishingHudWidget] Starting lure craft. LureId='{lure.LureId}', Recipe='{lure.CraftRecipeId}'.");
                var start = await _fishingHudActions.StartCraftAsync(lure.CraftRecipeId, ct);
                if (!start.Success)
                {
                    ShowInfo(GetCraftingErrorMessage(start.Error));
                    Debug.LogWarning($"[FishingHudWidget] Craft start rejected. LureId='{lure.LureId}', Recipe='{lure.CraftRecipeId}', Error={start.Error}.");
                    return;
                }

                SetActiveCraft(start.TaskId, start.CompleteAtUtc);
                SetProductionIconByCraftRecipeId(lure.CraftRecipeId);
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
                RefreshDragLocks();
                UpdateSpeedUpButtonState();
            }
        }

        private async UniTask RestoreActiveCraftAsync(CancellationToken ct)
        {
            if (_fishingHudActions == null)
                return;

            try
            {
                var activeTask = await _fishingHudActions.GetActiveCraftAsync(ct);
                if (activeTask == null)
                {
                    ClearActiveCraft();
                    return;
                }

                Debug.LogWarning($"[FishingHudWidget] Restoring active craft. TaskId='{activeTask.Id}', Recipe='{activeTask.Recipe?.Id}', CompleteAtUtc='{activeTask.CompleteAtUtc:O}'.");
                SetActiveCraft(activeTask.Id, activeTask.CompleteAtUtc);
                SetProductionIconByCraftRecipeId(activeTask.Recipe?.Id);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FishingHudWidget] Failed to restore active lure craft. {exception}");
            }
        }

        private void SetProductionIconByCraftRecipeId(string craftRecipeId)
        {
            if (string.IsNullOrWhiteSpace(craftRecipeId))
            {
                SetProductionIcon(null);
                return;
            }

            SetProductionIcon(_spritesByCraftRecipeId.TryGetValue(craftRecipeId, out var sprite) ? sprite : null);
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
            RefreshDragLocks();
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
            RefreshDragLocks();
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

        private async UniTask CollectActiveCraftAsync(bool requireReady, CancellationToken ct, int refundOnFailureGems = 0)
        {
            if (!_hasActiveCraft || _fishingHudActions == null)
                return;

            _isCraftOperationRunning = true;
            RefreshDragLocks();
            UpdateSpeedUpButtonState();

            try
            {
                Debug.LogWarning($"[FishingHudWidget] Collecting lure craft. TaskId='{_activeTaskId}', RequireReady={requireReady}.");
                var collect = requireReady
                    ? await _fishingHudActions.CollectAsync(_activeTaskId, ct)
                    : await _fishingHudActions.CompleteAndCollectAsync(_activeTaskId, ct);

                if (!collect.Success)
                {
                    Debug.LogWarning($"[FishingHudWidget] Craft collect failed. TaskId='{_activeTaskId}', Error={collect.Error}.");
                    if (refundOnFailureGems > 0)
                        await _fishingHudActions.RefundSpeedUpGemsAsync(refundOnFailureGems, ct);
                    return;
                }

                Debug.LogWarning($"[FishingHudWidget] Craft collected. TaskId='{_activeTaskId}', OutputItemId='{collect.OutputItemId}', OutputCount={collect.OutputCount}.");
                ClearActiveCraft();
                await RefreshLureCountsAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                if (refundOnFailureGems > 0)
                    await _fishingHudActions.RefundSpeedUpGemsAsync(refundOnFailureGems, ct);

                throw;
            }
            finally
            {
                _isCraftOperationRunning = false;
                RefreshDragLocks();
                UpdateSpeedUpButtonState();
            }
        }

        private async UniTask RefreshLureCountsAsync(CancellationToken ct)
        {
            if (_fishingHudActions == null)
                return;

            try
            {
                var renderData = await _fishingHudActions.GetLureRenderDataAsync(ct);
                RenderLureViews(renderData, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FishingHudWidget] Failed to refresh lure counts after craft completion. {exception}");
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

            if (_fishingHudActions == null)
            {
                Debug.LogWarning("[FishingHudWidget] Speed-up failed. Fishing HUD actions are not assigned.");
                return;
            }

            _isSpeedUpRunning = true;
            UpdateSpeedUpButtonState();

            try
            {
                Debug.LogWarning($"[FishingHudWidget] Speed-up requested for task '{_activeTaskId}'.");
                var spent = await _fishingHudActions.TrySpendSpeedUpGemsAsync(SpeedUpCost, ct);
                if (!spent)
                {
                    ShowInfo("Not enough gems.");
                    return;
                }

                await CollectActiveCraftAsync(requireReady: false, ct, refundOnFailureGems: SpeedUpCost);
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
                RefreshDragLocks();
                UpdateSpeedUpButtonState();
            }
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

        private DateTimeOffset GetCurrentTime()
        {
            return _fishingHudActions?.GetCurrentTimeUtc() ?? DateTimeOffset.UtcNow;
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
