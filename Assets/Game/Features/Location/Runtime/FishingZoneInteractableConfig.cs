using Fabros.TileEditor;
using UnityEngine;

namespace Game.Features.Locations
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LocationObject))]
    public sealed class FishingZoneInteractableConfig : MonoBehaviour
    {
        [SerializeField] private string _fishingConfigId;

        private LocationObject _locationObject;

        public string FishingConfigId => _fishingConfigId;

        private void Awake()
        {
            ResolveLocationObject();
            SubscribeToLocationObject();
        }

        private void OnDestroy()
        {
            if (_locationObject != null)
                _locationObject.OnInitDone -= InitializeFromLocationObject;
        }

        public void InitializeFromLocationObject(bool isEditor)
        {
            ResolveLocationObject();

            if (_locationObject != null
                && _locationObject.TryParseProperty<string>(LocationInteractionPropertyNames.FishingConfigId, out var fishingConfigId))
            {
                _fishingConfigId = fishingConfigId;
            }
        }

        public void SetFishingConfigId(string fishingConfigId)
        {
            _fishingConfigId = fishingConfigId;
        }

        private void ResolveLocationObject()
        {
            if (_locationObject == null)
                _locationObject = GetComponentInParent<LocationObject>();
        }

        private void SubscribeToLocationObject()
        {
            if (_locationObject == null)
                return;

            _locationObject.OnInitDone -= InitializeFromLocationObject;
            _locationObject.OnInitDone += InitializeFromLocationObject;
        }
    }
}
