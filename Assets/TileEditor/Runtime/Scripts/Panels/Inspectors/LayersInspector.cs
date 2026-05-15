using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TileEditor
{
    public class LayersInspector : MonoBehaviour
    {
        [SerializeField] private SimpleButton _closeLocationButton;
        [SerializeField] private Dropdown _layersDropDown;
        [SerializeField] private InputField _sortingOrderInputField;

        private TileEditor _tileEditor;

        private void Awake()
        {
            _closeLocationButton.Init(() => _tileEditor.DeselectCurrentTool());
            _tileEditor = GetComponentInParent<TileEditor>();
        }

        private void OnEnable()
        {
            _layersDropDown.onValueChanged.AddListener(OnLayerChanged);
            _sortingOrderInputField.onValueChanged.AddListener(OnSortingOrderChanged);
            
            DisplayTiles();
        }

        private void OnDisable()
        {
            _layersDropDown.onValueChanged.RemoveListener(OnLayerChanged);
            _sortingOrderInputField.onValueChanged.RemoveListener(OnSortingOrderChanged);
            
            DisplayTiles(true);
        }

        private void OnLayerChanged(int layerIndex) => DisplayTiles();

        private void OnSortingOrderChanged(string sortingOrder) => DisplayTiles();

        private void DisplayTiles(bool forceVisible = false)
        {
            if (!int.TryParse(_sortingOrderInputField.text, out var sortingOrder))
            {
                sortingOrder = 0;
            }
            
            var layerName = _layersDropDown.options[_layersDropDown.value].text;
                
            foreach (var locationObject in _tileEditor.CurrentLocation.IterateObjects())
            {
                var locationObjectSortingGroup = locationObject.GetComponentInChildren<SortingGroup>();
                if (locationObjectSortingGroup == null)
                {
                    Debug.LogError($"LocationObject: {locationObject.Name} has no sorting group.");
                    continue;
                }
                
                locationObject.gameObject.SetActive(
                    locationObjectSortingGroup.sortingLayerName == layerName && 
                    locationObjectSortingGroup.sortingOrder == sortingOrder ||
                    forceVisible);
            }   
        }
    }
}