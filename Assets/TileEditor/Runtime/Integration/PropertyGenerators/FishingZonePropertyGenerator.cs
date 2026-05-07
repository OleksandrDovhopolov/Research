using Game.Features.Locations;
using UnityEngine;

namespace Fabros.TileEditor
{
    [RequireComponent(typeof(LocationObject))]
    [RequireComponent(typeof(LocationInteractableView))]
    public sealed class FishingZonePropertyGenerator : LocationInteractionPropertyGenerator
    {
        //[SerializeField] private string _defaultInteractionId = "fishing_zone";
        [SerializeField] private string _defaultFishingConfigId;

        protected override string DefaultInteractionId => _defaultInteractionId;
        protected override LocationInteractionType DefaultInteractionType => LocationInteractionType.FishingZone;

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
