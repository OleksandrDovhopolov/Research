using Game.Features.Locations;
using UnityEngine;

namespace Fabros.TileEditor
{
    [RequireComponent(typeof(LocationObject))]
    [RequireComponent(typeof(LocationInteractableView))]
    public sealed class FishingZonePropertyGenerator : LocationInteractionPropertyGenerator
    {
        [SerializeField] private string _defaultFishingConfigId;

        protected override string DefaultInteractionId => _defaultInteractionId;
        protected override string DefaultInteractionKey => LocationInteractionKeys.FishingZone;

        protected override void Awake()
        {
            base.Awake();

            CreateStringProperty(
                LocationInteractionPropertyNames.FishingConfigId,
                _defaultFishingConfigId,
                View.SetFishingConfigId);
        }
    }
}
