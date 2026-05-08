using System.Collections.Generic;
using Game.Features.Locations;

namespace Fishing
{
    public sealed class LocationZoneInfoHudDefinitionRegistry
    {
        private readonly Dictionary<string, LocationZoneInfoHudDefinition> _definitions = new(System.StringComparer.Ordinal)
        {
            [LocationInteractionKeys.FishingZone] = new LocationZoneInfoHudDefinition(LocationInteractionKeys.FishingZone, "Info", true),
        };

        public bool TryGetDefinition(string interactionKey, out LocationZoneInfoHudDefinition definition)
        {
            return _definitions.TryGetValue(interactionKey ?? string.Empty, out definition);
        }
    }

    public readonly struct LocationZoneInfoHudDefinition
    {
        public LocationZoneInfoHudDefinition(string interactionKey, string label, bool isEnabled)
        {
            InteractionKey = interactionKey;
            Label = label;
            IsEnabled = isEnabled;
        }

        public string InteractionKey { get; }
        public string Label { get; }
        public bool IsEnabled { get; }
    }
}
