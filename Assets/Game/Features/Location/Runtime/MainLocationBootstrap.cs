using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
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
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _locationRoot;

        private RuntimeLocationObjectsFactory _locationObjectsFactory;
        private Location _location;
        private RuntimeOrthographicLocationCameraController _cameraController;

        public Location CurrentLocation => _location;
        public RuntimeOrthographicLocationCameraController CameraController => _cameraController;

        
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

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Debug.LogError("[Location] Camera is not assigned and Camera.main was not found.");
                return;
            }

            var locationModel = JsonConvert.DeserializeObject<LocationModel>(_mainLocationJson.text);
            if (locationModel == null)
            {
                Debug.LogError($"[Location] Failed to deserialize location json '{_mainLocationJson.name}'.");
                return;
            }

            EnsureLocationRoot();
            InitCameraController();

            _locationObjectsFactory = new RuntimeLocationObjectsFactory(_diContainer);
            _location = await Location.CreateAsync(
                _locationRoot,
                locationModel,
                _locationObjectsFactory,
                _tileEditorSettings,
                false,
                cancellationToken);
        }

        private void EnsureLocationRoot()
        {
            if (_locationRoot != null)
            {
                return;
            }

            _locationRoot = new GameObject("LocationRoot").transform;
        }

        private void InitCameraController()
        {
            _cameraController = _camera.GetComponent<RuntimeOrthographicLocationCameraController>();
            if (_cameraController == null)
            {
                _cameraController = _camera.gameObject.AddComponent<RuntimeOrthographicLocationCameraController>();
            }

            _cameraController.Init(_camera, _tileEditorSettings);
        }

        private void OnDestroy()
        {
            _locationObjectsFactory?.Dispose();
            _locationObjectsFactory = null;
        }
    }
}
