using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Fishing
{
    public sealed class FishingService : IFishingService
    {
        private readonly IFishingConfigProvider _configProvider;
        private readonly IFishSelector _fishSelector;
        private readonly IFishWeightService _fishWeightService;
        private readonly ICaughtFishService _caughtFishService;
        private readonly IFishingInventoryGateway _inventoryGateway;
        private readonly IActiveFishingEventsProvider _eventsProvider;
        private readonly Dictionary<string, FishingAttempt> _attempts = new(StringComparer.Ordinal);

        public FishingService(
            IFishingConfigProvider configProvider,
            IFishSelector fishSelector,
            IFishWeightService fishWeightService,
            ICaughtFishService caughtFishService,
            IFishingInventoryGateway inventoryGateway,
            IActiveFishingEventsProvider eventsProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _fishSelector = fishSelector ?? throw new ArgumentNullException(nameof(fishSelector));
            _fishWeightService = fishWeightService ?? throw new ArgumentNullException(nameof(fishWeightService));
            _caughtFishService = caughtFishService ?? throw new ArgumentNullException(nameof(caughtFishService));
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
            return await _caughtFishService.HandleCatchAsync(result, ct);
        }
    }
}
