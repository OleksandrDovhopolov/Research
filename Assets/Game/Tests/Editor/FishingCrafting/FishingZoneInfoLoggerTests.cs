using Game.Features.Locations;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class FishingZoneInfoLoggerTests
    {
        [Test]
        public void ResolveZoneId_UsesFishingZoneComponentWhenPresent()
        {
            var gameObject = new GameObject("FishingZoneInteractable");

            try
            {
                var resolver = new LocationFishingZoneIdResolver();
                var interactable = gameObject.AddComponent<LocationInteractableView>();
                var zoneConfig = gameObject.AddComponent<FishingZoneInteractableConfig>();
                interactable.SetInteractionId("interaction_zone");
                zoneConfig.SetFishingConfigId("config_zone");

                var zoneId = resolver.ResolveZoneId(interactable);

                Assert.That(zoneId, Is.EqualTo("config_zone"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ResolveZoneId_FallsBackToInteractionIdWhenFishingZoneComponentIsEmpty()
        {
            var gameObject = new GameObject("FishingZoneInteractable");

            try
            {
                var resolver = new LocationFishingZoneIdResolver();
                var interactable = gameObject.AddComponent<LocationInteractableView>();
                gameObject.AddComponent<FishingZoneInteractableConfig>();
                interactable.SetInteractionId("interaction_zone");

                var zoneId = resolver.ResolveZoneId(interactable);

                Assert.That(zoneId, Is.EqualTo("interaction_zone"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
