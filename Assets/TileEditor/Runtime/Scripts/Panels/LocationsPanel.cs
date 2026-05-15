using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class LocationsPanel : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------

        [SerializeField] private Transform _existsLocationContainer;
        [SerializeField] private SelectLocationButtons _existsLocationButtonsPrefab;
        [SerializeField] private InputField _createNewLocationInputField;
        [SerializeField] private Button _createNewLocationButton;
        [SerializeField] private SimpleButton _openGuideButton;

        //--------------------------------------------------------------------------------------------------------------------------

        private TileEditor _tileEditor;
        private readonly List<SelectLocationButtons> _existsLocationsButtons = new List<SelectLocationButtons>();

        //--------------------------------------------------------------------------------------------------------------------------

        void Awake()
        {
            _tileEditor = GetComponentInParent<TileEditor>();

            _createNewLocationButton.onClick.AddListener(CreateNewLocation);
            _openGuideButton.Init(() => Application.OpenURL("https://fabros.atlassian.net/wiki/spaces/MERGE/pages/3010625549/TileEditor"));

            UpdateExistsLocations();
        }

        void OnEnable() => UpdateExistsLocations();

        //--------------------------------------------------------------------------------------------------------------------------

        private void UpdateExistsLocations()
        {
            if (_tileEditor == null) return;

            _existsLocationsButtons.ForEach(btn => Destroy(btn.gameObject));
            _existsLocationsButtons.Clear();

            var locations = _tileEditor.GetAllLocationsNames();
            foreach (string location in locations)
            {
                var locationButtons = Instantiate(_existsLocationButtonsPrefab, _existsLocationContainer);
                locationButtons.Init(location, _tileEditor.LoadLocation, DeleteLocation);
                _existsLocationsButtons.Add(locationButtons);
            }
        }

        private void CreateNewLocation()
        {
            var newLocationName = _createNewLocationInputField.text;
            if (string.IsNullOrEmpty(newLocationName))
            {
                var button = new PopupButtonModel { buttonText = "Ok" };
                var editorDialog = new DialogPopupModel { button1 = button, mainText = "Enter new location name!" };
                _tileEditor.OpenPopup(editorDialog);
                return;
            }

            if (_existsLocationsButtons.Exists(btn => btn.LocationName == newLocationName))
            {
                var button = new PopupButtonModel { buttonText = "Ok" };
                var dialog = new DialogPopupModel { button1 = button, mainText = $"Location with name <{newLocationName}> already exists!" };
                _tileEditor.OpenPopup(dialog);
                return;
            }

            _createNewLocationInputField.text = "";
            _tileEditor.CreateLocation(newLocationName);
        }

        private void DeleteLocation(string locationName)
        {
            var noButton = new PopupButtonModel { buttonText = "No" };
            var yesButton = new PopupButtonModel { buttonText = "Yes", buttonAction = () => { DoDeleteLocation(locationName); } };
            var dialog = new DialogPopupModel { button1 = noButton, button2 = yesButton, mainText = $"Delete location <{locationName}>?" };
            _tileEditor.OpenPopup(dialog);
        }

        private void DoDeleteLocation(string locationName)
        {
            _tileEditor.DeleteLocation(locationName);
            var deletedLocation = _existsLocationsButtons.Find(btn => btn.LocationName == locationName);
            _existsLocationsButtons.Remove(deletedLocation);
            Destroy(deletedLocation.gameObject);
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
}
