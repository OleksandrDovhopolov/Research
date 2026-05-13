using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class ActiveLocationPanel : MonoBehaviour
    {
        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public const string TOGGLE_GRID_CELL_COORDS_BUTTON_NAME = "Toggle Grid <color=red>C</color>ell Coords";
        public const string TOGGLE_GRID_CELL_RENDERER_BUTTON = "Toggle Grid Cell <color=red>R</color>enderer";

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        [SerializeField] private Text _activeLocationName;

        [Header("Common tools")]
        [SerializeField] private GameObject _commonToolsPart;
        [SerializeField] private Transform _commonToolsTransform;

        [Header("Add object")]
        [SerializeField] private SimpleButton _closeAddObjectPartButton;
        [SerializeField] private SimpleButton _expandAllCategoriesButton;
        [SerializeField] private SimpleButton _collapseAllCategoriesButton;
        [SerializeField] private GameObject _addNewObjectPart;
        [SerializeField] private Transform _locationCategoriesContainer;
        [SerializeField] private ObjectsCategory _objectsCategoryPrefab;
        [SerializeField] private InputField _objectsFilterInputField;

        [Header("Locations inspector")]
        [SerializeField] private GameObject _locationInspectorPart;

        [Header("Groups inspector")]
        [SerializeField] private GameObject _groupsInspectorPart;
        
        [Header("Layers inspector")]
        [SerializeField] private GameObject _layersInspectorPart;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private TileEditor _tileEditor;
        private bool _objectsSetupComplete;
        private readonly List<string> _objectCategoriesNames = new List<string>();
        private readonly List<ObjectsCategory> _objectCategories = new List<ObjectsCategory>();

        private bool _gridRendererEnabled = true;
        private bool _gridCoordsEnabled;

        //--------------------------------------------------------------------------------------------------------------------------

        void Awake()
        {
            TrySetupPanel();

            TileEditor.OnToolChanged += ToolChangedHandler;
            _objectsFilterInputField.onValueChanged.AddListener(UpdateObjectsFilter);
        }

        void OnEnable()
        {
            if (_tileEditor == null) return;
            UpdateActiveLocationData();
        }

        private void UpdateActiveLocationData()
        {
            _activeLocationName.text = _tileEditor.CurrentLocation.Name;
            TrySetupPanel();
        }

        private void TrySetupPanel()
        {
            if (_objectsSetupComplete) return;

            _objectsSetupComplete = true;
            _tileEditor = TileEditor.GetInstance();
            SetupObjects();
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Custom buttons logic

        public void AddCustomButton(string buttonText, string buttonHintText, Action onClickAction)
        {
            TrySetupPanel();

            var newButton = _tileEditor.CreateButton(_commonToolsTransform, buttonText, () => onClickAction?.Invoke());
            newButton.SetHintText(buttonHintText);
        }

        public void AddCustomToolButton(string buttonText, string buttonHintText, Action<int, int> buttonAction, Action<int, int, bool> buttonHoverAction, Action onToolActivatedAction, Action onToolDeactivatedAction, bool allowDragInput)
        {
            TrySetupPanel();

            var newButton = _tileEditor.CreateToolButton(_commonToolsTransform, buttonText, buttonAction, buttonHoverAction, TileEditor.ToolKind.Custom, allowDragInput);
            newButton.OnSelected += onToolActivatedAction;
            newButton.OnDeselected += onToolDeactivatedAction;
            newButton.SetHintText(buttonHintText);
        }

        //--------------------------------------------------------------------------------------------------------------------------

        private void SetupObjects()
        {
            // base tool buttons
            _tileEditor.CreateButton(_commonToolsTransform, "Save", () => _tileEditor.SaveLocation());
            _tileEditor.CreateButton(_commonToolsTransform, "Close", () => _tileEditor.CloseLocation());

            _tileEditor.CreateDelimiter(_commonToolsTransform, 5);

            _tileEditor.CreateButton(_commonToolsTransform, TOGGLE_GRID_CELL_RENDERER_BUTTON, () =>
            {
                _gridRendererEnabled = !_gridRendererEnabled;
                _tileEditor.SetGridCellRendererEnabled(_gridRendererEnabled);
            });

            _tileEditor.CreateButton(_commonToolsTransform, TOGGLE_GRID_CELL_COORDS_BUTTON_NAME, () =>
            {
                _gridCoordsEnabled = !_gridCoordsEnabled;
                _tileEditor.SetGridCellCoordsEnabled(_gridCoordsEnabled);
            });

            _tileEditor.CreateDelimiter(_commonToolsTransform, 5);

            _tileEditor.CreateToolButton(_commonToolsTransform, "<color=red>I</color>nspector", _tileEditor.InspectCellObjects, null, TileEditor.ToolKind.InspectObjects);

            _tileEditor.CreateDelimiter(_commonToolsTransform, 5);

            _tileEditor.CreateToolButton(_commonToolsTransform, "Add New <color=red>O</color>bject", null, null, TileEditor.ToolKind.AddObject);

            // Add new object part

            _closeAddObjectPartButton.Init(() => _tileEditor.DeselectCurrentTool());

            IReadOnlyCollection<LocationObject> locationObjects = _tileEditor.GetAllLocationObjects();
            foreach (LocationObject locationObject in locationObjects)
            {
                if (_objectCategoriesNames.Contains(locationObject.Category)) continue;
                CreateObjectsCategroy(locationObject.Category, locationObjects.Where(obj => obj.Category == locationObject.Category));
            }

            _expandAllCategoriesButton.Init(() => _objectCategories.ForEach(oc => oc.Expand(true)));
            _collapseAllCategoriesButton.Init(() => _objectCategories.ForEach(oc => oc.Collapse(true)));

            _addNewObjectPart.gameObject.SetActive(false);

            // Location inspector

            _tileEditor.CreateToolButton(_commonToolsTransform, "<color=red>L</color>ocation Inspector", null, null, TileEditor.ToolKind.InspectLocation);

            // Groups inspector
            _tileEditor.CreateToolButton(_commonToolsTransform, "<color=red>G</color>roups Inspector", null, null, TileEditor.ToolKind.InspectGroups);

            //Layers inspector
            _tileEditor.CreateToolButton(_commonToolsTransform, "<color=red>L</color>ayers Inspector", null, null, TileEditor.ToolKind.InspectLayers);

            _tileEditor.CreateDelimiter(_commonToolsTransform, 5);
        }

        private void CreateObjectsCategroy(string categoryName, IEnumerable<LocationObject> categoryObjects)
        {
            var newCategory = Instantiate(_objectsCategoryPrefab, _locationCategoriesContainer);
            newCategory.Init(categoryName, categoryObjects, _tileEditor);
            _objectCategoriesNames.Add(categoryName);
            _objectCategories.Add(newCategory);
        }

        private void UpdateObjectsFilter(string filterValue)
        {
            _objectCategories.ForEach(oc => oc.ApplyFilter(filterValue));
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void ToolChangedHandler(ToolButton _)
        {
            _commonToolsPart.SetActive(
                _tileEditor.CurrentToolKind != TileEditor.ToolKind.AddObject 
                && _tileEditor.CurrentToolKind != TileEditor.ToolKind.InspectLocation
                && _tileEditor.CurrentToolKind != TileEditor.ToolKind.InspectGroups
                && _tileEditor.CurrentToolKind != TileEditor.ToolKind.InspectLayers);

            _addNewObjectPart.SetActive(_tileEditor.CurrentToolKind == TileEditor.ToolKind.AddObject);
            _locationInspectorPart.SetActive(_tileEditor.CurrentToolKind == TileEditor.ToolKind.InspectLocation);
            _groupsInspectorPart.SetActive(_tileEditor.CurrentToolKind == TileEditor.ToolKind.InspectGroups);
            _layersInspectorPart.SetActive(_tileEditor.CurrentToolKind == TileEditor.ToolKind.InspectLayers);
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}