using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using VContainer;

namespace Game.Fishing
{
    public sealed class FishingLureSelectionHudWidget : LureListHudWidgetBase
    {
        private readonly HashSet<string> _allowedLureIds = new(StringComparer.Ordinal);

        private IFishingLureSelectionHudActions _actions;
        private string _zoneId = string.Empty;
        private bool _isStartOperationRunning;

        [Inject]
        public void Install(IFishingLureSelectionHudActions actions, HudMissTapInputController missTapInputController)
        {
            _actions = actions;
            InstallBase(missTapInputController);
        }

        public UniTask RenderAsync(FishingLureSelectionRenderArgs args, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            _zoneId = args?.ZoneId ?? string.Empty;
            _allowedLureIds.Clear();
            if (args?.AllowedLureIds != null)
            {
                foreach (var lureId in args.AllowedLureIds)
                {
                    if (!string.IsNullOrWhiteSpace(lureId))
                        _allowedLureIds.Add(lureId);
                }
            }

            _isStartOperationRunning = false;
            RenderLureViews(args?.Lures, ct);
            RefreshDragLocks();
            return UniTask.CompletedTask;
        }

        protected override bool ShouldLockLure(FishingHudLureViewData lure)
        {
            return _isStartOperationRunning ||
                   lure == null ||
                   lure.Count <= 0 ||
                   string.IsNullOrWhiteSpace(lure.LureId) ||
                   !_allowedLureIds.Contains(lure.LureId);
        }

        protected override string GetLureDragBlockedMessage(FishingHudLureViewData lure)
        {
            if (_isStartOperationRunning)
                return "Fishing is already starting.";

            if (lure == null)
                return "Lure is not configured.";

            if (lure.Count <= 0)
                return "You do not have this lure.";

            if (string.IsNullOrWhiteSpace(lure.LureId) || !_allowedLureIds.Contains(lure.LureId))
                return "This lure cannot be used here.";

            return "Fishing cannot be started right now.";
        }

        protected override async UniTask HandleDroppedLureAsync(FishingHudLureViewData lure, CancellationToken ct)
        {
            if (lure == null || string.IsNullOrWhiteSpace(_zoneId))
                return;

            if (_actions == null)
            {
                ShowInfo("Fishing start service is not available.");
                return;
            }

            _isStartOperationRunning = true;
            RefreshDragLocks();

            try
            {
                var result = await _actions.TryStartFishingAsync(_zoneId, lure.LureId, ct);
                if (!result.Success)
                {
                    ShowInfo(MapErrorToMessage(result.Error));
                    return;
                }

                HideHud();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                ShowInfo("Failed to start fishing.");
                UnityEngine.Debug.LogError($"[FishingLureSelectionHudWidget] Start failed. ZoneId='{_zoneId}', LureId='{lure.LureId}'. {exception}");
            }
            finally
            {
                _isStartOperationRunning = false;
                RefreshDragLocks();
            }
        }

        protected override void ShowInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (_actions == null)
            {
                UnityEngine.Debug.LogWarning($"[FishingLureSelectionHudWidget] Actions are not assigned. Info='{message}'.");
                return;
            }

            _actions.ShowInfo(message);
        }

        protected override void HideHud()
        {
            if (_actions != null)
            {
                _actions.HideHud();
                return;
            }

            gameObject.SetActive(false);
        }

        protected override void OnDisposing()
        {
            _allowedLureIds.Clear();
            _zoneId = string.Empty;
            _isStartOperationRunning = false;
        }

        private static string MapErrorToMessage(FishingError error)
        {
            return error switch
            {
                FishingError.ZoneNotFound => "Fishing zone was not found.",
                FishingError.ZoneLocked => "This fishing zone is locked.",
                FishingError.LureNotFound => "Lure was not found.",
                FishingError.LureNotAllowedInZone => "This lure cannot be used here.",
                FishingError.LureNotInInventory => "You do not have this lure.",
                FishingError.InventoryOperationFailed => "Failed to spend the selected lure.",
                FishingError.ConfigInvalid => "Fishing configuration is invalid.",
                _ => "Failed to start fishing."
            };
        }
    }
}
