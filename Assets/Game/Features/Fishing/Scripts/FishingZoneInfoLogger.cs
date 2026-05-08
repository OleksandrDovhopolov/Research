using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Fishing
{
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

        public async UniTask LogZoneInfoAsync(string zoneId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(zoneId))
            {
                Debug.LogWarning("[FishingZone] Missing zone id.");
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

                Debug.LogWarning(builder.ToString());
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
    }
}
