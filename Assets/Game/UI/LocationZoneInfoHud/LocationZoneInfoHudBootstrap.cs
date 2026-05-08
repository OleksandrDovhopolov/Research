using Cysharp.Threading.Tasks;
using Game.Features.Locations;
using UnityEngine;

namespace UIShared
{
    public sealed class LocationZoneInfoHudBootstrap : MonoBehaviour, ILocationZoneInfoHudBootstrap
    {
        private LocationZoneInfoHudService _service;
        private bool _isInitializationStarted;

        public void Initialize(MainLocationBootstrap locationBootstrap)
        {
            _service ??= new LocationZoneInfoHudService(new LocationZoneInfoHudDefinitionRegistry());
            if (_isInitializationStarted)
                return;

            _isInitializationStarted = true;
            _service.InitializeAsync(locationBootstrap, transform, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnDestroy()
        {
            _service?.Dispose();
            _service = null;
            _isInitializationStarted = false;
        }
    }
}
