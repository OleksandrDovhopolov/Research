using System;
using System.Collections.Generic;
using UnityEngine;

namespace TileEditor
{
    public partial class TileEditor
    {
        public TileEditorSettings Settings { get; private set; }
        public bool SetupComplete { get; private set; }
        public event Action OnSetupComplete;

        public ILocationObjectsFactory LocationObjectsFactory { get; private set; }

        private List<LocationObject> _locationObjects;
        public IReadOnlyCollection<LocationObject> GetAllLocationObjects() => _locationObjects;

        public void SetupEditor(
            ILocationsSerializer locationsSerializer, 
            ILocationObjectsFactory factory, 
            ILocationObjectsGetter locationObjectsGetter, 
            TileEditorSettings settings)
        {
            if (SetupComplete)
            {
                Debug.LogWarning("[TileEditor] : tried to setup already setuped editor!");
                return;
            }

            _locationsSerializer = locationsSerializer;
            LocationObjectsFactory = factory;
            _locationObjects = locationObjectsGetter.GetAllLocationObjects();
            Settings = settings;

            Debug.Log($"[TileEditor] : setup complete. Found {_locationsSerializer.GetAllLocations().Count} exits locations and {_locationObjects.Count} available location objects.");

            GenerateGrid();
            _locationsPanel.SetActive(true);

            switch (settings.cameraType)
            {
                case CameraType.Orthographic2D:
                    _cameraController = gameObject.AddComponent<Orthographic2DCameraController>();
                    break;

                case CameraType.Perspective3D:
                    _cameraController = gameObject.AddComponent<Perspective3DCameraController>();
                    break;
            }
            _cameraController.Init(_cameraTransform, _camera);

            SetupComplete = true;
            OnSetupComplete?.Invoke();
        }
    }
}