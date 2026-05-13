using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Locations;
using UIShared;
using UnityEngine;
using VContainer;

namespace Game.Fishing
{
    public sealed class LocationZoneInfoHudBootstrap : MonoBehaviour
    {
        private readonly LocationZoneInfoHudDefinitionRegistry _registry = new();
        private readonly List<LocationZoneInfoHudItemView> _spawnedItems = new();

        private IHudController _hudController;
        private bool _isInitializationStarted;

        [Inject]
        public void Install(IHudController hudController, ILocationInteractablesSource locationSource)
        {
            if (_isInitializationStarted)
                return;

            if (hudController == null)
            {
                Debug.LogWarning("[ZoneInfoHud] HudController is not assigned.");
                return;
            }

            if (locationSource == null)
            {
                Debug.LogWarning("[ZoneInfoHud] Location interactables source is not assigned.");
                return;
            }

            _isInitializationStarted = true;
            _hudController = hudController;
            InitializeAsync(hudController, locationSource, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void OnDestroy()
        {
            ReleaseSpawnedItems();
            _hudController = null;
            _isInitializationStarted = false;
        }

        private async UniTaskVoid InitializeAsync(IHudController hudController, ILocationInteractablesSource locationSource, CancellationToken cancellationToken)
        {
            try
            {
                await locationSource.WaitForLocationAsync(cancellationToken);

                var hudWidget = await hudController.GetHudWidgetAsync<LocationZoneInfoHudWidget>(cancellationToken);
                if (hudWidget.ItemsRoot == null)
                {
                    Debug.LogWarning("[ZoneInfoHud] HUD widget items root is not assigned.");
                    return;
                }

                await SpawnItemsAsync(hudController, locationSource, hudWidget, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async UniTask SpawnItemsAsync(
            IHudController hudController,
            ILocationInteractablesSource locationSource,
            LocationZoneInfoHudWidget hudWidget,
            CancellationToken cancellationToken)
        {
            foreach (var interactable in locationSource.InteractionObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ShouldCreateItem(interactable, out var definition))
                    continue;

                var itemView = await hudController.CreateHudItemAsync<LocationZoneInfoHudItemView>(
                    LocationZoneInfoHudAddressables.LocationZoneInfoHudItemPrefab,
                    hudWidget.ItemsRoot,
                    cancellationToken);

                //itemView.name = $"ZoneInfo_{interactable.InteractionId}";
                itemView.transform.SetPositionAndRotation(interactable.HudAnchor.position, Quaternion.identity);
                _spawnedItems.Add(itemView);
                itemView.Initialize(interactable, definition.Label);
            }
        }

        private bool ShouldCreateItem(ILocationInteractable interactable, out LocationZoneInfoHudDefinition definition)
        {
            definition = default;

            if (interactable == null || !interactable.IsInteractionEnabled)
                return false;

            if (!_registry.TryGetDefinition(interactable.InteractionKey, out definition) || !definition.IsEnabled)
                return false;

            return interactable.HudAnchor != null;
        }

        private void ReleaseSpawnedItems()
        {
            foreach (var item in _spawnedItems)
                _hudController?.ReleaseHudItem(item);

            _spawnedItems.Clear();
        }
    }
}
