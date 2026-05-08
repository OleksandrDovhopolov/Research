using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Locations;
using UnityEngine;
using VContainer;

namespace UIShared
{
    public sealed class LocationZoneInfoHudBootstrap : MonoBehaviour
    {
        private LocationZoneInfoHudService _service;
        private bool _isInitializationStarted;

        [Inject]
        public void Install(IHudController hudController, MainLocationBootstrap locationBootstrap)
        {
            if (_isInitializationStarted)
                return;

            if (hudController == null)
            {
                Debug.LogWarning("[ZoneInfoHud] HudController is not assigned.");
                return;
            }

            if (locationBootstrap == null)
            {
                Debug.LogWarning("[ZoneInfoHud] MainLocationBootstrap is not assigned.");
                return;
            }

            _isInitializationStarted = true;
            InitializeAsync(hudController, locationBootstrap, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnDestroy()
        {
            _service?.Dispose();
            _service = null;
            _isInitializationStarted = false;
        }

        private async UniTaskVoid InitializeAsync(IHudController hudController, MainLocationBootstrap locationBootstrap, CancellationToken cancellationToken)
        {
            try
            {
                var hudWidget = await hudController.GetHudWidgetAsync<LocationZoneInfoHudWidget>(cancellationToken);

                _service ??= new LocationZoneInfoHudService(new LocationZoneInfoHudDefinitionRegistry());
                await _service.InitializeAsync(locationBootstrap, hudWidget, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
