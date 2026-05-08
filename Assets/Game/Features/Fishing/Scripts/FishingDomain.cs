using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Locations;

namespace Game.Fishing
{
    public enum FishingError
    {
        None = 0,
        ZoneNotFound,
        ZoneLocked,
        ZoneNotInteractive,
        LureNotFound,
        LureNotAllowedInZone,
        LureNotInInventory,
        NoAvailableFish,
        EventRequired,
        MinigameFailed,
        ConfigInvalid,
        AttemptNotFound,
        InventoryOperationFailed
    }

    public enum FishWeightState
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    public readonly struct FishingAttemptId
    {
        public FishingAttemptId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }
        public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
        public override string ToString() => Value;
    }

    public sealed class FishingAttempt
    {
        public FishingAttempt(FishingAttemptId id, string zoneId, string lureId, FishConfig selectedFish)
        {
            Id = id;
            ZoneId = zoneId;
            LureId = lureId;
            SelectedFish = selectedFish;
        }

        public FishingAttemptId Id { get; }
        public string ZoneId { get; }
        public string LureId { get; }
        public FishConfig SelectedFish { get; }
    }

    public sealed class FishingStartResult
    {
        public bool Success { get; private set; }
        public FishingError Error { get; private set; }
        public FishingAttemptId AttemptId { get; private set; }
        public FishConfig SelectedFish { get; private set; }

        public static FishingStartResult Ok(FishingAttempt attempt)
        {
            return new FishingStartResult
            {
                Success = true,
                Error = FishingError.None,
                AttemptId = attempt.Id,
                SelectedFish = attempt.SelectedFish
            };
        }

        public static FishingStartResult Fail(FishingError error)
        {
            return new FishingStartResult
            {
                Success = false,
                Error = error,
                AttemptId = new FishingAttemptId(string.Empty)
            };
        }
    }

    public sealed class FishingCatchResult
    {
        public bool Success { get; private set; }
        public FishingError Error { get; private set; }
        public string FishId { get; private set; }
        public string ItemId { get; private set; }
        public float Weight { get; private set; }
        public FishWeightState State { get; private set; }

        public static FishingCatchResult Ok(string fishId, string itemId, float weight, FishWeightState state)
        {
            return new FishingCatchResult
            {
                Success = true,
                Error = FishingError.None,
                FishId = fishId,
                ItemId = itemId,
                Weight = weight,
                State = state
            };
        }

        public static FishingCatchResult Fail(FishingError error)
        {
            return new FishingCatchResult
            {
                Success = false,
                Error = error
            };
        }
    }

    public sealed class FishWeightRollResult
    {
        public FishWeightRollResult(float weight, FishWeightState state)
        {
            Weight = weight;
            State = state;
        }

        public float Weight { get; }
        public FishWeightState State { get; }
    }

    public sealed class FishBookProgress
    {
        public string FishId;
        public bool IsDiscovered;
        public bool IsNew;
        public int CaughtCount;
        public float BestWeight;
        public List<string> UnlockedWeightStates = new();
    }

    public interface IFishingService
    {
        UniTask<FishingStartResult> StartFishingAsync(string zoneId, string lureId, CancellationToken ct = default);
        UniTask<FishingCatchResult> CompleteFishingAsync(FishingAttemptId attemptId, bool minigameSuccess, CancellationToken ct = default);
    }

    public interface IFishSelector
    {
        IReadOnlyList<FishConfig> GetAvailableFish(
            IReadOnlyList<FishConfig> fish,
            string lureId,
            string waterBodyType,
            IReadOnlyCollection<string> activeEventIds);

        FishConfig SelectFish(
            IReadOnlyList<FishConfig> fish,
            string lureId,
            string waterBodyType,
            IReadOnlyCollection<string> activeEventIds);
    }

    public interface IFishWeightService
    {
        FishWeightRollResult RollWeight(FishConfig fishConfig);
        FishWeightState GetState(FishConfig fishConfig, float weight);
    }

    public interface IFishBookService
    {
        UniTask RegisterCatchAsync(FishingCatchResult result, CancellationToken ct = default);
        UniTask<FishBookProgress> GetProgressAsync(string fishId, CancellationToken ct = default);
        UniTask MarkAsViewedAsync(string fishId, CancellationToken ct = default);
    }

    public interface IFishingInventoryGateway
    {
        UniTask<bool> HasItemAsync(string itemId, int amount, CancellationToken ct = default);
        UniTask<bool> RemoveItemAsync(string itemId, int amount, CancellationToken ct = default);
        UniTask AddItemAsync(string itemId, int amount, CancellationToken ct = default);
    }

    public interface IActiveFishingEventsProvider
    {
        IReadOnlyCollection<string> GetActiveEventIds();
    }

    public interface IFishingZoneInfoLogger
    {
        UniTask LogZoneInfoAsync(string zoneId, CancellationToken ct = default);
    }

    public interface IFishingRandom
    {
        double NextDouble();
    }

    public sealed class SystemFishingRandom : IFishingRandom
    {
        private readonly Random _random = new();

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }

    public sealed class EmptyActiveFishingEventsProvider : IActiveFishingEventsProvider
    {
        private static readonly string[] Empty = Array.Empty<string>();

        public IReadOnlyCollection<string> GetActiveEventIds()
        {
            return Empty;
        }
    }
}
