using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Fishing
{
    public sealed class FishingLureSelectionStartService : IFishingLureSelectionStartService
    {
        private readonly IFishingConfigProvider _configProvider;
        private readonly IFishingInventoryGateway _inventoryGateway;

        public FishingLureSelectionStartService(
            IFishingConfigProvider configProvider,
            IFishingInventoryGateway inventoryGateway)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _inventoryGateway = inventoryGateway ?? throw new ArgumentNullException(nameof(inventoryGateway));
        }

        public async UniTask<FishingLureSelectionStartResult> TryStartAsync(string zoneId, string lureId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(zoneId))
                return FishingLureSelectionStartResult.Fail(FishingError.ZoneNotFound);

            if (string.IsNullOrWhiteSpace(lureId))
                return FishingLureSelectionStartResult.Fail(FishingError.LureNotFound);

            var data = await _configProvider.LoadAsync(ct);
            if (!data.ZonesById.TryGetValue(zoneId, out var zone))
                return FishingLureSelectionStartResult.Fail(FishingError.ZoneNotFound);

            if (zone.IsUnlockFeatureEnabled && !zone.IsUnlockedByDefault)
                return FishingLureSelectionStartResult.Fail(FishingError.ZoneLocked);

            if (!data.LuresById.TryGetValue(lureId, out var lure))
                return FishingLureSelectionStartResult.Fail(FishingError.LureNotFound);

            if (zone.AllowedLureIds == null || !zone.AllowedLureIds.Contains(lureId))
                return FishingLureSelectionStartResult.Fail(FishingError.LureNotAllowedInZone);

            if (string.IsNullOrWhiteSpace(lure.ItemId))
                return FishingLureSelectionStartResult.Fail(FishingError.ConfigInvalid);

            if (!await _inventoryGateway.HasItemAsync(lure.ItemId, 1, ct))
                return FishingLureSelectionStartResult.Fail(FishingError.LureNotInInventory);

            if (!await _inventoryGateway.RemoveItemAsync(lure.ItemId, 1, ct))
                return FishingLureSelectionStartResult.Fail(FishingError.InventoryOperationFailed);

            Debug.LogWarning($"[FishingLureSelectionStartService] Fishing start stub succeeded. ZoneId='{zoneId}', LureId='{lureId}', ItemId='{lure.ItemId}'.");
            return FishingLureSelectionStartResult.Ok();
        }
    }
}
