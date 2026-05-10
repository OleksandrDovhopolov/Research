using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIShared;
using UISystem;
using UnityEngine;

namespace Game.Fishing
{
    public interface IFishingMinigameFacade
    {
        UniTask<bool> TryShowAsync(string zoneId, CancellationToken ct = default);
    }

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
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(zoneId))
            {
                ShowInfo("Fishing zone is unavailable.");
                return false;
            }

            FishingStartResult startResult;
            try
            {
                startResult = await _fishingService.StartFishingAsync(zoneId, DefaultLureId, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingMinigameFacade] Failed to start fishing in zone '{zoneId}'. {exception}");
                ShowInfo("Fishing is temporarily unavailable.");
                return false;
            }

            if (startResult == null || !startResult.Success)
            {
                ShowInfo(GetStartErrorMessage(startResult?.Error ?? FishingError.ConfigInvalid));
                return false;
            }

            var runtimeConfig = await BuildRuntimeConfigAsync(startResult.SelectedFish, ct);
            var args = new FishingMinigameArgs(zoneId, startResult.AttemptId, startResult.SelectedFish, runtimeConfig);

            try
            {
                await _uiManager.GetWindowAsync<FishingMinigameController>();
                _uiManager.Show<FishingMinigameController>(args);
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

            _uiManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        private async UniTask FailAttemptSafelyAsync(FishingAttemptId attemptId)
        {
            if (attemptId.IsEmpty)
                return;

            try
            {
                await _fishingService.CompleteFishingAsync(attemptId, false, CancellationToken.None);
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
                    return "You need a gold lure to fish here.";
                case FishingError.NoAvailableFish:
                    return "No fish are biting right now.";
                case FishingError.EventRequired:
                    return "A fishing event is required for this catch.";
                default:
                    return "Fishing is temporarily unavailable.";
            }
        }
    }

    public sealed class FishingMinigameArgs : WindowArgs
    {
        public FishingMinigameArgs(
            string zoneId,
            FishingAttemptId attemptId,
            FishConfig selectedFish,
            FishingMinigameRuntimeConfig runtimeConfig)
        {
            ZoneId = zoneId ?? string.Empty;
            AttemptId = attemptId;
            SelectedFish = selectedFish;
            RuntimeConfig = runtimeConfig;
        }

        public string ZoneId { get; }
        public FishingAttemptId AttemptId { get; }
        public FishConfig SelectedFish { get; }
        public FishingMinigameRuntimeConfig RuntimeConfig { get; }
    }

    public readonly struct FishingMinigameRuntimeConfig
    {
        public FishingMinigameRuntimeConfig(
            float startRadius,
            float targetRadius,
            float endRadius,
            float shrinkDurationSeconds,
            float successRadiusThreshold,
            float perfectRadiusThreshold)
        {
            StartRadius = startRadius;
            TargetRadius = targetRadius;
            EndRadius = endRadius;
            ShrinkDurationSeconds = shrinkDurationSeconds;
            SuccessRadiusThreshold = successRadiusThreshold;
            PerfectRadiusThreshold = perfectRadiusThreshold;
        }

        public float StartRadius { get; }
        public float TargetRadius { get; }
        public float EndRadius { get; }
        public float ShrinkDurationSeconds { get; }
        public float SuccessRadiusThreshold { get; }
        public float PerfectRadiusThreshold { get; }
    }

    public readonly struct FishingMinigameResolution
    {
        public FishingMinigameResolution(bool isSuccess, bool isPerfect, bool isTimeout, float currentRadius)
        {
            IsSuccess = isSuccess;
            IsPerfect = isPerfect;
            IsTimeout = isTimeout;
            CurrentRadius = currentRadius;
        }

        public bool IsSuccess { get; }
        public bool IsPerfect { get; }
        public bool IsTimeout { get; }
        public float CurrentRadius { get; }
    }
}
