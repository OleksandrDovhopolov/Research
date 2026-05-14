using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using UISystem;
using UnityEngine;

namespace Game.Fishing
{
    public sealed class FishingMinigameFacade : IFishingMinigameFacade
    {
        private const string DefaultLureId = "gold_lure";

        private readonly IFishingService _fishingService;
        private readonly IFishingConfigProvider _configProvider;
        private readonly UIManager _uiManager;

        public FishingMinigameFacade(
            IFishingService fishingService,
            IFishingConfigProvider configProvider,
            UIManager uiManager)
        {
            _fishingService = fishingService ?? throw new ArgumentNullException(nameof(fishingService));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
        }

        public async UniTask<bool> TryShowAsync(string zoneId, CancellationToken ct = default)
        {
            return await TryShowAsync(zoneId, DefaultLureId, ct);
        }

        public async UniTask<bool> TryShowAsync(string zoneId, string lureId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var resolvedLureId = string.IsNullOrWhiteSpace(lureId) ? DefaultLureId : lureId;
            Debug.LogWarning($"[FishingMinigameFacade] TryShow requested. ZoneId='{zoneId}', LureId='{resolvedLureId}'.");

            if (string.IsNullOrWhiteSpace(zoneId))
            {
                Debug.LogWarning("[FishingMinigameFacade] TryShow rejected because zone id is empty.");
                ShowInfo("Fishing zone is unavailable.");
                return false;
            }

            FishingStartResult startResult;
            try
            {
                startResult = await _fishingService.StartFishingAsync(zoneId, resolvedLureId, ct);
                Debug.LogWarning($"[FishingMinigameFacade] StartFishingAsync finished. ZoneId='{zoneId}', LureId='{resolvedLureId}', Success={startResult?.Success ?? false}, Error={startResult?.Error ?? FishingError.ConfigInvalid}, AttemptId='{startResult?.AttemptId.ToString() ?? string.Empty}', FishId='{startResult?.SelectedFish?.Id ?? string.Empty}'.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[FishingMinigameFacade] TryShow cancelled during StartFishingAsync. ZoneId='{zoneId}', LureId='{resolvedLureId}'.");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingMinigameFacade] Failed to start fishing in zone '{zoneId}' with lure '{resolvedLureId}'. {exception}");
                ShowInfo("Fishing is temporarily unavailable.");
                return false;
            }

            if (startResult == null || !startResult.Success)
            {
                Debug.LogWarning($"[FishingMinigameFacade] TryShow failed after start. ZoneId='{zoneId}', LureId='{resolvedLureId}', Error={startResult?.Error ?? FishingError.ConfigInvalid}.");
                ShowInfo(GetStartErrorMessage(startResult?.Error ?? FishingError.ConfigInvalid));
                return false;
            }

            var runtimeConfig = await BuildRuntimeConfigAsync(startResult.SelectedFish, ct);
            Debug.LogWarning($"[FishingMinigameFacade] Runtime config prepared. ZoneId='{zoneId}', AttemptId='{startResult.AttemptId}', FishId='{startResult.SelectedFish?.Id ?? string.Empty}', Behavior='{startResult.SelectedFish?.BehaviorType ?? string.Empty}', StartRadius={runtimeConfig.StartRadius:0.##}, TargetRadius={runtimeConfig.TargetRadius:0.##}, EndRadius={runtimeConfig.EndRadius:0.##}, Duration={runtimeConfig.ShrinkDurationSeconds:0.###}, SuccessThreshold={runtimeConfig.SuccessRadiusThreshold:0.###}, PerfectThreshold={runtimeConfig.PerfectRadiusThreshold:0.###}.");
            var args = new FishingMinigameArgs(zoneId, startResult.AttemptId, startResult.SelectedFish, runtimeConfig);

            try
            {
                await _uiManager.GetWindowAsync<FishingMinigameController>();
                _uiManager.Show<FishingMinigameController>(args);
                Debug.LogWarning($"[FishingMinigameFacade] FishingMinigameController show dispatched. ZoneId='{zoneId}', AttemptId='{startResult.AttemptId}', FishId='{startResult.SelectedFish?.Id ?? string.Empty}'.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingMinigameFacade] Failed to open fishing minigame for zone '{zoneId}'. {exception}");
                await FailAttemptSafelyAsync(startResult.AttemptId);
                ShowInfo("Fishing UI failed to open.");
                return false;
            }
        }

        private async UniTask<FishingMinigameRuntimeConfig> BuildRuntimeConfigAsync(FishConfig selectedFish, CancellationToken ct)
        {
            var data = await _configProvider.LoadAsync(ct);
            var minigame = data?.Settings?.FishingMinigame;
            var behaviorSettings = data?.Settings?.FishBehaviorSettings;
            var behavior = behaviorSettings?
                .FirstOrDefault(x => x != null && string.Equals(x.Id, selectedFish?.BehaviorType, StringComparison.Ordinal));

            var startRadius = Mathf.Max(1f, minigame?.StartRadius ?? 220f);
            var targetRadius = Mathf.Max(1f, minigame?.TargetRadius ?? 90f);
            var endRadius = Mathf.Max(1f, minigame?.EndRadius ?? 40f);
            var baseDuration = Mathf.Max(0.05f, minigame?.ShrinkDurationSeconds ?? 2.5f);
            var baseTolerance = Mathf.Max(0.01f, minigame?.SuccessTolerance ?? 0.12f);

            var speedMultiplier = behavior?.MinigameSpeedMultiplier > 0f ? behavior.MinigameSpeedMultiplier : 1f;
            var toleranceMultiplier = behavior?.SuccessToleranceMultiplier > 0f ? behavior.SuccessToleranceMultiplier : 1f;

            var shrinkDuration = Mathf.Max(0.05f, baseDuration * speedMultiplier);
            var successThreshold = Mathf.Max(1f, targetRadius * baseTolerance * toleranceMultiplier);
            var perfectThreshold = Mathf.Max(0.5f, successThreshold * 0.5f);

            return new FishingMinigameRuntimeConfig(
                startRadius,
                targetRadius,
                endRadius,
                shrinkDuration,
                successThreshold,
                perfectThreshold);
        }

        private void ShowInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            Debug.LogWarning($"[FishingMinigameFacade] ShowInfo: '{message}'.");
            _uiManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        private async UniTask FailAttemptSafelyAsync(FishingAttemptId attemptId)
        {
            if (attemptId.IsEmpty)
                return;

            try
            {
                Debug.LogWarning($"[FishingMinigameFacade] Cancelling fishing attempt after UI failure. AttemptId='{attemptId}'.");
                await _fishingService.CompleteFishingAsync(attemptId, false, CancellationToken.None);
                Debug.LogWarning($"[FishingMinigameFacade] Fishing attempt cancelled after UI failure. AttemptId='{attemptId}'.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[FishingMinigameFacade] Failed to cancel fishing attempt '{attemptId}'. {exception}");
            }
        }

        private static string GetStartErrorMessage(FishingError error)
        {
            switch (error)
            {
                case FishingError.ZoneNotFound:
                case FishingError.ZoneNotInteractive:
                    return "Fishing zone is unavailable.";
                case FishingError.ZoneLocked:
                    return "This fishing zone is locked.";
                case FishingError.LureNotFound:
                case FishingError.LureNotAllowedInZone:
                    return "The selected lure cannot be used here.";
                case FishingError.LureNotInInventory:
                    return "You do not have the selected lure.";
                case FishingError.NoAvailableFish:
                    return "No fish are biting right now.";
                case FishingError.EventRequired:
                    return "A fishing event is required for this catch.";
                default:
                    return "Fishing is temporarily unavailable.";
            }
        }
    }
}
