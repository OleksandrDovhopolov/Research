using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Locations;
using Infrastructure;
using UnityEngine;

namespace UIShared
{
    public sealed class LocationZoneInfoHudService : IDisposable
    {
        private readonly LocationZoneInfoHudDefinitionRegistry _registry;
        private readonly List<GameObject> _spawnedInstances = new();

        private MainLocationBootstrap _locationBootstrap;
        private Transform _parentTransform;
        private GameObject _prefab;
        private bool _isInitialized;
        private bool _isInitializing;

        public LocationZoneInfoHudService(LocationZoneInfoHudDefinitionRegistry registry)
        {
            _registry = registry;
        }

        public async UniTask InitializeAsync(
            MainLocationBootstrap locationBootstrap,
            Transform parentTransform,
            CancellationToken cancellationToken)
        {
            if (_isInitialized || _isInitializing)
                return;

            _isInitializing = true;
            _locationBootstrap = locationBootstrap;
            _parentTransform = parentTransform;

            try
            {
                if (_locationBootstrap == null)
                {
                    Debug.LogWarning("[ZoneInfoHud] MainLocationBootstrap is not assigned.");
                    return;
                }

                if (_parentTransform == null)
                {
                    Debug.LogWarning("[ZoneInfoHud] Parent transform is not assigned.");
                    return;
                }

                await WaitForLocationAsync(cancellationToken);
                _prefab = await ProdAddressablesWrapper.LoadAsync<GameObject>(LocationZoneInfoHudAddressables.LocationZoneInfoHudPrefab, cancellationToken);

                SpawnItems();
                _isInitialized = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public void Dispose()
        {
            foreach (var instance in _spawnedInstances)
            {
                if (instance != null)
                    UnityEngine.Object.Destroy(instance);
            }

            _spawnedInstances.Clear();

            if (_prefab != null)
            {
                ProdAddressablesWrapper.Release(_prefab);
                _prefab = null;
            }

            _locationBootstrap = null;
            _parentTransform = null;
            _isInitialized = false;
            _isInitializing = false;
        }

        private void SpawnItems()
        {
            if (_prefab == null)
                return;

            foreach (var interactable in _locationBootstrap.InteractionObjects)
            {
                if (interactable == null || !interactable.IsInteractionEnabled)
                    continue;

                if (!_registry.TryGetDefinition(interactable.InteractionKey, out var definition) || !definition.IsEnabled)
                    continue;

                if (interactable.HudAnchor == null)
                    continue;

                CreateItem(interactable, definition);
            }
        }

        private void CreateItem(ILocationInteractable interactable, LocationZoneInfoHudDefinition definition)
        {
            var spawnPosition = interactable.HudAnchor.position;
            var itemObject = UnityEngine.Object.Instantiate(_prefab, spawnPosition, Quaternion.identity, _parentTransform);
            itemObject.name = $"ZoneInfo_{interactable.InteractionId}";

            var itemView = itemObject.GetComponent<LocationZoneInfoHudItemView>();
            if (itemView == null)
            {
                Debug.LogWarning($"[ZoneInfoHud] Prefab '{LocationZoneInfoHudAddressables.LocationZoneInfoHudPrefab}' does not contain {nameof(LocationZoneInfoHudItemView)}.");
                UnityEngine.Object.Destroy(itemObject);
                return;
            }

            _spawnedInstances.Add(itemObject);
            itemView.Initialize(interactable, definition.Label);
        }

        private async UniTask WaitForLocationAsync(CancellationToken cancellationToken)
        {
            while (_locationBootstrap != null && _locationBootstrap.CurrentLocation == null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}
