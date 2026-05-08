using System;
using System.Collections.Generic;
using System.Linq;

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
}
