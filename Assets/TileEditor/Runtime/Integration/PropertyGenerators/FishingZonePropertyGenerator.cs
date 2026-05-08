using Game.Features.Locations;
using UnityEngine;

namespace Fabros.TileEditor
{
    [RequireComponent(typeof(LocationObject))]
    [RequireComponent(typeof(LocationInteractableView))]
    [RequireComponent(typeof(FishingZoneInteractableConfig))]
    public sealed class FishingZonePropertyGenerator : LocationInteractionPropertyGenerator
    {
        [SerializeField] private string _defaultFishingConfigId;

        private FishingZoneInteractableConfig _zoneConfig;

        protected override string DefaultInteractionId => _defaultInteractionId;
        protected override string DefaultInteractionKey => LocationInteractionKeys.FishingZone;

        protected override void Awake()
        {
            base.Awake();
            _zoneConfig = GetComponent<FishingZoneInteractableConfig>();

            CreateStringProperty(
                LocationInteractionPropertyNames.FishingConfigId,
                _defaultFishingConfigId,
                _zoneConfig.SetFishingConfigId);
        }
    }
}
