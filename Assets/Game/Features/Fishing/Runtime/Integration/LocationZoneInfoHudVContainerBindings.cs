using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Fishing
{
    public static class LocationZoneInfoHudVContainerBindings
    {
        public static void RegisterLocationZoneInfoHud(this IContainerBuilder builder, LocationZoneInfoHudBootstrap bootstrap)
        {
            if (bootstrap != null)
            {
                builder.RegisterComponent(bootstrap);
                return;
            }

            Debug.LogWarning("[LocationZoneInfoHud] LocationZoneInfoHudBootstrap reference is not assigned.");
        }
    }
}
