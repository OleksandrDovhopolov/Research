using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fabros.TileEditor;
using Newtonsoft.Json;
using UnityEngine;
using VContainer;

namespace Game.Features.Locations
{
    public sealed class MainLocationBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset _mainLocationJson;
        [SerializeField] private TileEditorSettings _tileEditorSettings;
        [SerializeField] private Transform _locationRoot;

        private RuntimeLocationObjectsFactory _locationObjectsFactory;
        private Location _location;
        private readonly List<ILocationInteractable> _interactionObjects = new();

        public Location CurrentLocation => _location;
        public IReadOnlyList<ILocationInteractable> InteractionObjects => _interactionObjects;

        private IObjectResolver _diContainer;
        
        [Inject]
        private void Construct(IObjectResolver diContainer)
        {
            _diContainer = diContainer;
        }
        
        private void Start()
        {
            LoadAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid LoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                await CreateLocationAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private async UniTask CreateLocationAsync(CancellationToken cancellationToken)
        {
            if (_mainLocationJson == null)
            {
                Debug.LogError("[Location] Main location json is not assigned.");
                return;
            }

            if (_tileEditorSettings == null)
            {
                Debug.LogError("[Location] TileEditorSettings is not assigned.");
                return;
            }

            var locationModel = JsonConvert.DeserializeObject<LocationModel>(_mainLocationJson.text);
            if (locationModel == null)
            {
                Debug.LogError($"[Location] Failed to deserialize location json '{_mainLocationJson.name}'.");
                return;
            }

            EnsureLocationRoot();
            _interactionObjects.Clear();

            _locationObjectsFactory = new RuntimeLocationObjectsFactory(_diContainer);
            _location = await Location.CreateAsync(
                _locationRoot,
                locationModel,
                _locationObjectsFactory,
                _tileEditorSettings,
                false,
                cancellationToken,
                OnLocationObjectCreated);
        }

        private void EnsureLocationRoot()
        {
            if (_locationRoot != null)
            {
                return;
            }

            _locationRoot = new GameObject("LocationRoot").transform;
        }

        private void OnDestroy()
        {
            _interactionObjects.Clear();
            _locationObjectsFactory?.Dispose();
            _locationObjectsFactory = null;
        }

        private void OnLocationObjectCreated(LocationObject locationObject)
        {
            if (locationObject == null)
                return;

            var interactable = locationObject.GetComponent<LocationInteractableView>();
            if (interactable != null)
                _interactionObjects.Add(interactable);
        }
    }
}
