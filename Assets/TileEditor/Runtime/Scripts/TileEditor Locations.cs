using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Fabros.TileEditor
{
    public partial class TileEditor
    {
        //--------------------------------------------------------------------------------------------------------------------------

        public event Action OnLocationChanged; 

        [SerializeField] private Transform _locationContainer;
        private ILocationsSerializer _locationsSerializer;

        //--------------------------------------------------------------------------------------------------------------------------

        private Location _currentLocation;

        public Location CurrentLocation 
        { 
            get => _currentLocation;
            private set
            {
                if (value == _currentLocation) return;

                ResetCameraPosition();
                CurrentToolButton = null;
                CurrentToolKind = ToolKind.None;

                var locationExists = value != null;
                if (!locationExists && _currentLocation != null) Destroy(_currentLocation.gameObject);
                _currentLocation = value;

                _locationsPanel.SetActive(!locationExists);
                _activeLocationPanel.gameObject.SetActive(locationExists);

                ClearCommandsLists();

                OnLocationChanged?.Invoke();
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public IReadOnlyCollection<string> GetAllLocationsNames() => _locationsSerializer.GetAllLocations();

        public void LoadLocation(string locationName)
        {
            var serializedLocation = _locationsSerializer.LoadLocation(locationName);
            var locationModel = JsonConvert.DeserializeObject<LocationModel>(serializedLocation);
            GenerateLocation(locationModel);
        }

        public void CreateLocation(string locationName)
        {
            GenerateLocation(new LocationModel {name = locationName, objects = new List<LocationObjectModel>()});
        }

        public void SaveLocation()
        {
            if (CurrentLocation == null) return;

            LocationModel locationModel = CurrentLocation.SaveLocation();
            _locationsSerializer.SaveLocation(CurrentLocation.Name, JsonConvert.SerializeObject(locationModel));
        }

        public void CloseLocation()
        {
            CurrentLocation = null;
        }

        public void DeleteLocation(string locationName)
        {
            _locationsSerializer.DeleteLocation(locationName);
        }

        //--------------------------------------------------------------------------------------------------------------------------

        private async void GenerateLocation(LocationModel locationModel)
        {
            CurrentLocation = await Location.Create(_locationContainer, locationModel, LocationObjectsFactory, Settings, true);
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
}