using UnityEngine;

namespace UIShared
{
    public sealed class LocationZoneInfoHudBootstrap : MonoBehaviour, ILocationZoneInfoHudBootstrap
    {
        private LocationZoneInfoHudService _service;

        public void Initialize(MonoBehaviour locationBootstrap)
        {
            _service ??= new LocationZoneInfoHudService(new LocationZoneInfoHudDefinitionRegistry());
            _service.Initialize(locationBootstrap);
            enabled = true;
        }

        private void Update()
        {
            if (_service == null)
                return;

            if (_service.TryInitialize())
                enabled = false;
        }

        private void OnDestroy()
        {
            _service?.Dispose();
            _service = null;
        }
    }
}
