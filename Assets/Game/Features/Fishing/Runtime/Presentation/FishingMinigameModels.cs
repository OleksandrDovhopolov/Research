using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;

namespace Game.Fishing
{
    public interface IFishingMinigameFacade
    {
        UniTask<bool> TryShowAsync(string zoneId, CancellationToken ct = default);
        UniTask<bool> TryShowAsync(string zoneId, string lureId, CancellationToken ct = default);
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

    public enum FishingMinigameEndReason
    {
        SuccessfulTap = 0,
        MissedTap = 1,
        EarlyTap = 2,
        Timeout = 3
    }

    public readonly struct FishingMinigameResolution
    {
        public FishingMinigameResolution(
            bool isSuccess,
            bool isPerfect,
            bool isTimeout,
            float currentRadius,
            FishingMinigameEndReason endReason)
        {
            IsSuccess = isSuccess;
            IsPerfect = isPerfect;
            IsTimeout = isTimeout;
            CurrentRadius = currentRadius;
            EndReason = endReason;
        }

        public bool IsSuccess { get; }
        public bool IsPerfect { get; }
        public bool IsTimeout { get; }
        public float CurrentRadius { get; }
        public FishingMinigameEndReason EndReason { get; }
    }
}
