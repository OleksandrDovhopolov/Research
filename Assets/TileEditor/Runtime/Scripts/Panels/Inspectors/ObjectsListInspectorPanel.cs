using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class ObjectsListInspectorPanel : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------------------------------

        private enum SubPanel { None, Properies, Groups, Tools }

        private SubPanel activeSubPanel;

        //--------------------------------------------------------------------------------------------------------------------------

        [SerializeField] private Transform _objectsInspectorsLiteContainer;

        [SerializeField] private BooleanPropertyEditor _booleanPropertyEditorPrefab;
        [SerializeField] private IntegerPropertyEditor _integerPropertyEditorPrefab;
        [SerializeField] private FloatPropertyEditor _floatPropertyEditorPrefab;
        [SerializeField] private FloatRangedPropertyEditor _floatRangedPropertyEditorPrefab;
        [SerializeField] private IntegerRangedPropertyEditor _integerRangedPropertyEditorPrefab;
        [SerializeField] private EnumPropertyEditor _enumPropertyEditorPrefab;
        [SerializeField] private StringPropertyEditor _stringPropertyEditorPrefab;
        [SerializeField] private ObjectReferencePropertyEditor _objectReferencePropertyPrefab;

        [SerializeField] private ObjectInspectorLite _objectInspectorLitePrefab;
        [SerializeField] private GameObject _selectionEmptyInfoText;


        [Header("Properies")]
        [SerializeField] private SimpleButton _togglePropertiesButton;
        [SerializeField] private Transform _editorsContainer;
        [SerializeField] private GameObject _properiesSubPanel;
        [SerializeField] private GameObject _noSimularProperiesText;

        [Header("Properies")]
        [SerializeField] private SimpleButton _toggleToolsButton;
        [SerializeField] private Transform _commonToolsContainer;

        [Header("Groups")]
        [SerializeField] private SimpleButton _toggleGroupsButton;
        [SerializeField] private GameObject _groupsSubPanelSubPanel;
        [SerializeField] private GameObject _addToGroupContainer;
        [SerializeField] private SimpleButton _addToGroupButton;
        [SerializeField] private Dropdown _addToGroupDropdown;
        [SerializeField] private Transform _objectGroupViewContainer;
        [SerializeField] private ObjectGroupView _objectGroupViewPrefab;
        private List<string> _currentGroupsOptions;

        private readonly List<ObjectInspectorLite> _currentObjectsInspectors = new List<ObjectInspectorLite>();
        private List<BasePropertyEditor> _currentProperyEditors;
        private List<ObjectGroupView> _currentGroupViews;

        private List<LocationObject> _inspectedObjects;
        private TileEditor _tileEditor;
        private SimpleButton _selectAsBrushButton;

        private bool HasInspectedObjects => _inspectedObjects != null && _inspectedObjects.Count > 0;

        void Awake()
        {
            _tileEditor = GetComponentInParent<TileEditor>();

            _selectAsBrushButton = _tileEditor.CreateButton(_commonToolsContainer, "Select As Brush", SelectObjectAsBrush);
            _selectAsBrushButton.gameObject.SetActive(false);

            _tileEditor.CreateButton(_commonToolsContainer, "↑ Move Up", () => MoveSelection(Vector2Int.up));
            _tileEditor.CreateButton(_commonToolsContainer, "→ Move Right", () => MoveSelection(Vector2Int.right));
            _tileEditor.CreateButton(_commonToolsContainer, "↓ Move Down", () => MoveSelection(Vector2Int.down));
            _tileEditor.CreateButton(_commonToolsContainer, "← Move Left", () => MoveSelection(Vector2Int.left));

            _tileEditor.CreateButton(_commonToolsContainer, "Highlight All", HighlightObjects);
            _tileEditor.CreateButton(_commonToolsContainer, "Close", _tileEditor.DeselectCurrentTool);

            _togglePropertiesButton.Init(() => ToggleSubPanel(SubPanel.Properies));
            _toggleToolsButton.Init(() => ToggleSubPanel(SubPanel.Tools));
            _toggleGroupsButton.Init(() => ToggleSubPanel(SubPanel.Groups));

            _tileEditor.OnCommandApplied += UpdateInspectedObjects;
            _tileEditor.OnCommandReverted += UpdateInspectedObjects;

            _addToGroupButton.Init(TryAddToGroup);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                _currentObjectsInspectors.ForEach(oi => oi.SetRemoveButtonText("[Delete]"));
            }

            if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                _currentObjectsInspectors.ForEach(oi => oi.SetRemoveButtonText("[Unselect]"));
            }
        }

        private void UpdateInspectedObjects()
        {
            if (!HasInspectedObjects) return;

            _inspectedObjects.RemoveAll(io => io == null);
            Inspect(_inspectedObjects);
        }

        private void SelectObjectAsBrush()
        {
            _tileEditor.SelectObjectAsBrush(_inspectedObjects[0]);
        }

        private void HighlightObjects()
        {
            _inspectedObjects?.ForEach(io => io.Highlight());
        }

        public void Inspect(LocationObject locationObject) => Inspect(new List<LocationObject> { locationObject });

        public void Inspect(List<LocationObject> inspectedObjects)
        {
            ClearCurrentObjectInspectors();
            ClearCurrentEditors();
            ClearGroupsInfo();

            _inspectedObjects = inspectedObjects;

            if (!HasInspectedObjects)
            {
                _selectionEmptyInfoText.SetActive(true);
                return;
            }

            _selectAsBrushButton.gameObject.SetActive(_inspectedObjects.Count == 1);

            _selectionEmptyInfoText.SetActive(false);

            // create lite objects inspectors
            foreach (var inspectedObject in _inspectedObjects)
            {
                var objectInspector = Instantiate(_objectInspectorLitePrefab, _objectsInspectorsLiteContainer);
                objectInspector.Init(inspectedObject, () =>
                {
                    _inspectedObjects.Remove(inspectedObject);
                    if (Input.GetKey(KeyCode.LeftControl))
                        _tileEditor.ExecuteCommand(new RemoveObjectCommand(_tileEditor.CurrentLocation, inspectedObject));

                    Inspect(_inspectedObjects);
                }, () =>
                {
                    if (Input.GetKey(KeyCode.LeftControl)) inspectedObject.Highlight();
                    else Inspect(inspectedObject);
                });

                _currentObjectsInspectors.Add(objectInspector);
            }

            switch (activeSubPanel)
            {
                case SubPanel.None: break;
                case SubPanel.Properies: ShowProperiesEditors(); break;
                case SubPanel.Groups: ShowGroupsDropdown(); break;
                case SubPanel.Tools: break;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Properties logic

        private void ShowProperiesEditors()
        {
            _properiesSubPanel.SetActive(true);

            if (_currentProperyEditors != null || !HasInspectedObjects) return;

            _currentProperyEditors = new List<BasePropertyEditor>();

            // create list of properties, which exists in all group objects
            var sameProperties = new List<BaseProperty>();

            foreach (var property in _inspectedObjects[0].GetComponentsInChildren<BaseProperty>())
            {
                var propertyName = property.GetName();
                var propertyType = property.GetType();

                if (property is EnumProperty enumProperty)
                {
                    if (_inspectedObjects.TrueForAll(lo =>
                    {
                        bool hasProperty = lo.TryGetProperty(propertyName, propertyType, out var enumProp);
                        if (!hasProperty || ! (enumProp is EnumProperty otherEnum)) return false;
                        return otherEnum.GetEnumValuesNames().All(enumValuesName => enumProperty.GetEnumValuesNames().Contains(enumValuesName));
                    }))
                        sameProperties.Add(property);
                }
                else if (property is StringOptionsProperty stringOptionsProperty)
                {
                    if (_inspectedObjects.TrueForAll(lo =>
                    {
                        bool hasProperty = lo.TryGetProperty(propertyName, propertyType, out var optionsProp);
                        if (!hasProperty || !(optionsProp is StringOptionsProperty otherOptions)) return false;
                        return otherOptions.GetOptions().SequenceEqual(stringOptionsProperty.GetOptions());
                    }))
                        sameProperties.Add(property);
                }
                else
                {
                    if (_inspectedObjects.TrueForAll(lo => lo.TryGetProperty(propertyName, propertyType, out _)))
                        sameProperties.Add(property);
                }
            }

            // create editors 
            foreach (var property in sameProperties)
            {
                var propertyName = property.GetName();
                var propertyType = property.GetType();

                switch (property)
                {
                    case BooleanProperty booleanProperty:
                        var booleanEditor = Instantiate(_booleanPropertyEditorPrefab, _editorsContainer);
                        booleanEditor.Init(
                            booleanProperty.GetName(),
                            booleanProperty.GetGenericValue(),
                            b => SetObjectsPropertiesValue(propertyName, propertyType, b));

                        _currentProperyEditors.Add(booleanEditor);
                        break;

                    case IntegerProperty integerProperty:
                        var integerEditor = Instantiate(_integerPropertyEditorPrefab, _editorsContainer);
                        integerEditor.Init(
                            integerProperty.GetName(),
                            integerProperty.GetGenericValue(),
                            i => SetObjectsPropertiesValue(propertyName, propertyType, i));

                        _currentProperyEditors.Add(integerEditor);
                        break;

                    case FloatProperty floatProperty:
                        var floatEditor = Instantiate(_floatPropertyEditorPrefab, _editorsContainer);
                        floatEditor.Init(
                            floatProperty.GetName(),
                            floatProperty.GetGenericValue(),
                            f => SetObjectsPropertiesValue(propertyName, propertyType, f));

                        _currentProperyEditors.Add(floatEditor);
                        break;

                    case FloatRangedProperty floatRangedProperty:
                        var floatRangedEditor = Instantiate(_floatRangedPropertyEditorPrefab, _editorsContainer);
                        floatRangedEditor.Init(
                            floatRangedProperty.GetName(),
                            floatRangedProperty.GetMinValue(),
                            floatRangedProperty.GetMaxValue(),
                            floatRangedProperty.GetGenericValue(),
                            f => SetObjectsPropertiesValue(propertyName, propertyType, f));

                        _currentProperyEditors.Add(floatRangedEditor);
                        break;

                    case IntegerRangedProperty integerRangedProperty:
                        var integerRangedEditor = Instantiate(_integerRangedPropertyEditorPrefab, _editorsContainer);
                        integerRangedEditor.Init(
                            integerRangedProperty.GetName(),
                            integerRangedProperty.GetMinValue(),
                            integerRangedProperty.GetMaxValue(),
                            integerRangedProperty.GetGenericValue(),
                            i => SetObjectsPropertiesValue(propertyName, propertyType, i));

                        _currentProperyEditors.Add(integerRangedEditor);
                        break;

                    case EnumProperty enumProperty:
                        var enumEditor = Instantiate(_enumPropertyEditorPrefab, _editorsContainer);
                        enumEditor.Init(
                            enumProperty.GetName(),
                            enumProperty.GetEnumValuesNames(),
                            (int)enumProperty.GetValue(),
                            i => SetObjectsPropertiesValue(propertyName, propertyType, i));

                        _currentProperyEditors.Add(enumEditor);
                        break;

                    case StringOptionsProperty stringOptionsProperty:
                        var stringOptionsEditor = Instantiate(_enumPropertyEditorPrefab, _editorsContainer);
                        stringOptionsEditor.Init(
                            stringOptionsProperty.GetName(),
                            stringOptionsProperty.GetOptions(),
                            stringOptionsProperty.GetSelectedIndex(),
                            i => SetObjectsPropertiesValue(propertyName, propertyType, i));

                        _currentProperyEditors.Add(stringOptionsEditor);
                        break;

                    case StringProperty stringProperty:
                        var stringEditor = Instantiate(_stringPropertyEditorPrefab, _editorsContainer);
                        stringEditor.Init(
                            stringProperty.GetName(),
                            stringProperty.GetGenericValue(),
                            s => SetObjectsPropertiesValue(propertyName, propertyType, s));

                        _currentProperyEditors.Add(stringEditor);
                        break;

                    case ObjectReferenceProperty objectReferenceProperty:
                        var referenceEditor = Instantiate(_objectReferencePropertyPrefab, _editorsContainer);
                        referenceEditor.Init(
                            objectReferenceProperty.GetName(),
                            objectReferenceProperty.GetGenericValue(),
                            s => SetObjectsPropertiesValue(propertyName, propertyType, s));

                        _currentProperyEditors.Add(referenceEditor);
                        break;
                }
            }

            _noSimularProperiesText.gameObject.SetActive(_currentProperyEditors.Count == 0);
        }

        private void SetObjectsPropertiesValue(string propertyName, Type propertyType, object value)
        {
            _inspectedObjects.ForEach(io =>
            {
                io.TryGetProperty(propertyName, propertyType, out var objectProperty);
                objectProperty?.SetValue(value);
            });
        }

        private void ClearCurrentObjectInspectors()
        {
            _currentObjectsInspectors.ForEach(oi => Destroy(oi.gameObject));
            _currentObjectsInspectors.Clear();
        }

        private void ClearCurrentEditors()
        {
            if (_currentProperyEditors == null) return;

            _noSimularProperiesText.gameObject.SetActive(false);
            _currentProperyEditors.ForEach(bpe => Destroy(bpe.gameObject));
            _currentProperyEditors = null;
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Groups logic

        private void ShowGroupsDropdown()
        {
            _groupsSubPanelSubPanel.SetActive(true);

            if (_currentGroupViews != null || !HasInspectedObjects) return;

            _currentGroupViews = new List<ObjectGroupView>();

            // get same groups for all selected objects:
            var sameGroups = new List<string>();

            foreach (var group in _inspectedObjects[0].IterateGroups())
            {
                if (_inspectedObjects.TrueForAll(lo => lo.IsInGroup(group)))
                    sameGroups.Add(group);
            }

            _currentGroupsOptions = _tileEditor
                .CurrentLocation
                .IterateGroups()
                .Where(gr => !sameGroups.Contains(gr))
                .OrderBy(gr => gr).ToList();

            _addToGroupContainer.SetActive(_currentGroupsOptions.Count > 0);
            _addToGroupDropdown.AddOptions(_currentGroupsOptions);

            foreach (string groupName in sameGroups.OrderBy(gr => gr))
            {
                var gv = Instantiate(_objectGroupViewPrefab, _objectGroupViewContainer);
                gv.Init(groupName, () =>
                {
                    _tileEditor.ExecuteCommand(new RemoveFromGroupCommand(_inspectedObjects, groupName));
                });

                _currentGroupViews.Add(gv);
            }
        }

        private void TryAddToGroup()
        {
            _tileEditor.ExecuteCommand(new AddToGroupCommand(_inspectedObjects, GetCurrentSelectedGroup()));
        }

        private string GetCurrentSelectedGroup() => _currentGroupsOptions[_addToGroupDropdown.value];

        private void ClearGroupsInfo()
        {
            if (_currentGroupViews == null) return;

            _currentGroupViews.ForEach(cgv => { if (cgv != null) Destroy(cgv.gameObject); });
            _currentGroupViews = null;

            _addToGroupDropdown.ClearOptions();
        }

        //--------------------------------------------------------------------------------------------------------------------------
        // Common tools

        private void MoveSelection(Vector2Int direction)
        {
            if (_inspectedObjects.TrueForAll(io => io.Cell.GetCellInDirection(direction).CanAddObject(io)))
            {
                _tileEditor.ExecuteCommand(new MoveObjectsCommand(_inspectedObjects, direction));
            };
        }

        //--------------------------------------------------------------------------------------------------------------------------

        private void ToggleSubPanel(SubPanel subPanel)
        {
            _properiesSubPanel.SetActive(false);
            _commonToolsContainer.gameObject.SetActive(false);
            _groupsSubPanelSubPanel.SetActive(false);

            if (subPanel == activeSubPanel)
            {
                activeSubPanel = SubPanel.None;
                return;
            }

            activeSubPanel = subPanel;
            switch (activeSubPanel)
            {
                case SubPanel.Properies: ShowProperiesEditors(); break;
                case SubPanel.Groups: ShowGroupsDropdown(); break;
                case SubPanel.Tools: _commonToolsContainer.gameObject.SetActive(true); break;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
}
