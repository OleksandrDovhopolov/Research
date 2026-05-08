using UnityEngine;

namespace Game.Features.Locations
{
    public sealed class LocationFishingZoneIdResolver : ILocationFishingZoneIdResolver
    {
        public string ResolveZoneId(ILocationInteractable interactable)
        {
            if (interactable == null)
                return string.Empty;

            var zoneConfig = (interactable as Component)?.GetComponent<FishingZoneInteractableConfig>();
            return zoneConfig != null && !string.IsNullOrWhiteSpace(zoneConfig.FishingConfigId)
                ? zoneConfig.FishingConfigId
                : interactable.InteractionId;
        }
    }
}
