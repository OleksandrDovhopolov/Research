using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Locations;
using UnityEngine;

namespace Game.Fishing
{
    public sealed class FishSelector : IFishSelector
    {
        private readonly IFishingRandom _random;

        public FishSelector(IFishingRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public IReadOnlyList<FishConfig> GetAvailableFish(
            IReadOnlyList<FishConfig> fish,
            string lureId,
            string waterBodyType,
            IReadOnlyCollection<string> activeEventIds)
        {
            if (fish == null || string.IsNullOrWhiteSpace(lureId) || string.IsNullOrWhiteSpace(waterBodyType))
                return Array.Empty<FishConfig>();

            var activeEvents = new HashSet<string>(activeEventIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var result = new List<FishConfig>();
            for (var i = 0; i < fish.Count; i++)
            {
                var config = fish[i];
                if (config == null || config.SpawnWeight <= 0)
                    continue;

                if (config.AvailableLureIds == null || !config.AvailableLureIds.Contains(lureId))
                    continue;

                if (config.WaterBodyTypes == null || !config.WaterBodyTypes.Contains(waterBodyType))
                    continue;

                if (config.EventOnly && !HasActiveEvent(config.EventIds, activeEvents))
                    continue;

                result.Add(config);
            }

            return result;
        }

        public FishConfig SelectFish(
            IReadOnlyList<FishConfig> fish,
            string lureId,
            string waterBodyType,
            IReadOnlyCollection<string> activeEventIds)
        {
            var available = GetAvailableFish(fish, lureId, waterBodyType, activeEventIds);
            if (available.Count == 0)
                return null;

            var totalWeight = 0;
            for (var i = 0; i < available.Count; i++)
                totalWeight += Math.Max(0, available[i].SpawnWeight);

            if (totalWeight <= 0)
                return null;

            var roll = _random.NextDouble() * totalWeight;
            var cumulative = 0;
            for (var i = 0; i < available.Count; i++)
            {
                cumulative += Math.Max(0, available[i].SpawnWeight);
                if (roll < cumulative)
                    return available[i];
            }

            return available[available.Count - 1];
        }

        private static bool HasActiveEvent(IReadOnlyList<string> fishEventIds, HashSet<string> activeEvents)
        {
            if (fishEventIds == null || fishEventIds.Count == 0 || activeEvents.Count == 0)
                return false;

            for (var i = 0; i < fishEventIds.Count; i++)
            {
                if (activeEvents.Contains(fishEventIds[i]))
                    return true;
            }

            return false;
        }
    }

    public sealed class FishWeightService : IFishWeightService
    {
        private readonly IFishingRandom _random;

        public FishWeightService(IFishingRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public FishWeightRollResult RollWeight(FishConfig fishConfig)
        {
            if (fishConfig?.WeightThresholds == null)
                return new FishWeightRollResult(0f, FishWeightState.Common);

            var min = Math.Max(0.01f, fishConfig.WeightThresholds.Common * 0.75f);
            var max = Math.Max(min, fishConfig.WeightThresholds.Legendary * 1.25f);
            var weight = min + (float)_random.NextDouble() * (max - min);
            var rounded = (float)Math.Round(weight, 2, MidpointRounding.AwayFromZero);
            return new FishWeightRollResult(rounded, GetState(fishConfig, rounded));
        }

        public FishWeightState GetState(FishConfig fishConfig, float weight)
        {
            var thresholds = fishConfig?.WeightThresholds;
            if (thresholds == null)
                return FishWeightState.Common;

            if (weight >= thresholds.Legendary)
                return FishWeightState.Legendary;
            if (weight >= thresholds.Epic)
                return FishWeightState.Epic;
            if (weight >= thresholds.Rare)
                return FishWeightState.Rare;

            return FishWeightState.Common;
        }
    }

    public sealed class FishingService : IFishingService
    {
        private readonly IFishingConfigProvider _configProvider;
        private readonly IFishSelector _fishSelector;
        private readonly IFishWeightService _fishWeightService;
        private readonly IFishBookService _fishBookService;
        private readonly IFishingInventoryGateway _inventoryGateway;
        private readonly IActiveFishingEventsProvider _eventsProvider;
        private readonly Dictionary<string, FishingAttempt> _attempts = new(StringComparer.Ordinal);

        public FishingService(
            IFishingConfigProvider configProvider,
            IFishSelector fishSelector,
            IFishWeightService fishWeightService,
            IFishBookService fishBookService,
            IFishingInventoryGateway inventoryGateway,
            IActiveFishingEventsProvider eventsProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _fishSelector = fishSelector ?? throw new ArgumentNullException(nameof(fishSelector));
            _fishWeightService = fishWeightService ?? throw new ArgumentNullException(nameof(fishWeightService));
            _fishBookService = fishBookService ?? throw new ArgumentNullException(nameof(fishBookService));
            _inventoryGateway = inventoryGateway ?? throw new ArgumentNullException(nameof(inventoryGateway));
            _eventsProvider = eventsProvider ?? throw new ArgumentNullException(nameof(eventsProvider));
        }

        public async UniTask<FishingStartResult> StartFishingAsync(string zoneId, string lureId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(zoneId))
                return FishingStartResult.Fail(FishingError.ZoneNotFound);

            if (string.IsNullOrWhiteSpace(lureId))
                return FishingStartResult.Fail(FishingError.LureNotFound);

            var data = await _configProvider.LoadAsync(ct);
            if (!data.ZonesById.TryGetValue(zoneId, out var zone))
                return FishingStartResult.Fail(FishingError.ZoneNotFound);

            if (zone.IsUnlockFeatureEnabled && !zone.IsUnlockedByDefault)
                return FishingStartResult.Fail(FishingError.ZoneLocked);

            if (!data.LuresById.TryGetValue(lureId, out var lure))
                return FishingStartResult.Fail(FishingError.LureNotFound);

            if (zone.AllowedLureIds == null || !zone.AllowedLureIds.Contains(lureId))
                return FishingStartResult.Fail(FishingError.LureNotAllowedInZone);

            if (!await _inventoryGateway.HasItemAsync(lure.ItemId, 1, ct))
                return FishingStartResult.Fail(FishingError.LureNotInInventory);

            var activeEventIds = _eventsProvider.GetActiveEventIds();
            var selectedFish = _fishSelector.SelectFish(data.Fish, lureId, zone.WaterBodyType, activeEventIds);
            if (selectedFish == null)
                return FishingStartResult.Fail(FishingError.NoAvailableFish);

            if (!await _inventoryGateway.RemoveItemAsync(lure.ItemId, 1, ct))
                return FishingStartResult.Fail(FishingError.InventoryOperationFailed);

            var attempt = new FishingAttempt(
                new FishingAttemptId(Guid.NewGuid().ToString("N")),
                zone.Id,
                lure.Id,
                selectedFish);
            _attempts[attempt.Id.Value] = attempt;

            return FishingStartResult.Ok(attempt);
        }

        public async UniTask<FishingCatchResult> CompleteFishingAsync(FishingAttemptId attemptId, bool minigameSuccess, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (attemptId.IsEmpty || !_attempts.TryGetValue(attemptId.Value, out var attempt))
                return FishingCatchResult.Fail(FishingError.AttemptNotFound);

            _attempts.Remove(attemptId.Value);

            if (!minigameSuccess)
                return FishingCatchResult.Fail(FishingError.MinigameFailed);

            var roll = _fishWeightService.RollWeight(attempt.SelectedFish);
            var itemId = FishingStaticData.GetFishItemId(attempt.SelectedFish.Id);
            await _inventoryGateway.AddItemAsync(itemId, 1, ct);

            var result = FishingCatchResult.Ok(attempt.SelectedFish.Id, itemId, roll.Weight, roll.State);
            await _fishBookService.RegisterCatchAsync(result, ct);
            return result;
        }
    }

    public sealed class FishingZoneInfoLogger : IFishingZoneInfoLogger
    {
        private readonly IFishingConfigProvider _configProvider;
        private readonly IFishSelector _fishSelector;
        private readonly IActiveFishingEventsProvider _eventsProvider;

        public FishingZoneInfoLogger(
            IFishingConfigProvider configProvider,
            IFishSelector fishSelector,
            IActiveFishingEventsProvider eventsProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _fishSelector = fishSelector ?? throw new ArgumentNullException(nameof(fishSelector));
            _eventsProvider = eventsProvider ?? throw new ArgumentNullException(nameof(eventsProvider));
        }

        public async UniTask LogZoneInfoAsync(ILocationInteractable interactable, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var zoneId = ResolveZoneId(interactable);
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                Debug.LogWarning("[FishingZone] Missing zone id on interactable.");
                return;
            }

            try
            {
                var data = await _configProvider.LoadAsync(ct);
                if (!data.ZonesById.TryGetValue(zoneId, out var zone))
                {
                    Debug.LogWarning($"[FishingZone] Zone '{zoneId}' not found in config.");
                    return;
                }

                var activeEvents = _eventsProvider.GetActiveEventIds();
                var builder = new StringBuilder();
                builder.Append("[FishingZone] ");
                builder.Append("zoneId='").Append(zone.Id).Append("', ");
                builder.Append("display_name='").Append(zone.DisplayName).Append("', ");
                builder.Append("water_body_type='").Append(zone.WaterBodyType).Append("', ");
                builder.Append("available=").Append(!zone.IsUnlockFeatureEnabled || zone.IsUnlockedByDefault).Append(", ");
                builder.Append("allowed_lure_ids=[").Append(string.Join(", ", zone.AllowedLureIds ?? new List<string>())).Append("]");

                if (zone.AllowedLureIds != null && zone.AllowedLureIds.Count > 0)
                {
                    builder.Append(", fish_counts={");
                    for (var i = 0; i < zone.AllowedLureIds.Count; i++)
                    {
                        var lureId = zone.AllowedLureIds[i];
                        var count = _fishSelector.GetAvailableFish(data.Fish, lureId, zone.WaterBodyType, activeEvents).Count;
                        if (i > 0)
                            builder.Append(", ");
                        builder.Append(lureId).Append(":").Append(count);
                    }

                    builder.Append("}");
                }

                Debug.Log(builder.ToString());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingZone] Failed to log zone '{zoneId}': {exception.Message}");
            }
        }

        public static string ResolveZoneId(ILocationInteractable interactable)
        {
            if (interactable == null)
                return string.Empty;

            return !string.IsNullOrWhiteSpace(interactable.FishingConfigId)
                ? interactable.FishingConfigId
                : interactable.InteractionId;
        }
    }
}
