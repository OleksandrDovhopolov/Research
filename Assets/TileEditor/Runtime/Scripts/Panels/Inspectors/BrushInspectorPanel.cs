using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class BrushInspectorPanel : MonoBehaviour
    {
        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        [SerializeField] private Text _inspectedObjectNameText;
        [SerializeField] private Transform _editorsContainer;

        [SerializeField] private BooleanPropertyEditor _booleanPropertyEditorPrefab;
        [SerializeField] private IntegerPropertyEditor _integerPropertyEditorPrefab;
        [SerializeField] private FloatPropertyEditor _floatPropertyEditorPrefab;
        [SerializeField] private FloatRangedPropertyEditor _floatRangedPropertyEditorPrefab;
        [SerializeField] private IntegerRangedPropertyEditor _integerRangedPropertyEditorPrefab;
        [SerializeField] private EnumPropertyEditor _enumPropertyEditorPrefab;
        [SerializeField] private StringPropertyEditor _stringPropertyEditorPrefab;

        [Header("Groups logic")]
        [SerializeField] private SimpleButton _addToGroupButton;
        [SerializeField] private Dropdown _addToGroupDropdown;
        [SerializeField] private Transform _objectGroupViewContainer;
        [SerializeField] private ObjectGroupView _objectGroupViewPrefab;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private readonly List<BasePropertyEditor> _currentEditors = new List<BasePropertyEditor>();
        private readonly List<ObjectGroupView> _currentGroupViews = new List<ObjectGroupView>();

        private LocationObject _inspectedObject;
        private TileEditor _tileEditor;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        void Awake()
        {
            _tileEditor = GetComponentInParent<TileEditor>();
            _addToGroupButton.Init(TryAddToGroup);
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void InitBrush(LocationObject brushObject)
        {
            if (brushObject == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _inspectedObject = brushObject;

            ClearCurrentEditors();
            UpdateGroupsDropdown();

            _inspectedObjectNameText.text = brushObject.Name;

            // create editors for components
            foreach (var property in brushObject.GetComponentsInChildren<BaseProperty>())
            {
                switch (property)
                {
                    case BooleanProperty booleanProperty:
                        var booleanEditor = Instantiate(_booleanPropertyEditorPrefab, _editorsContainer);
                        booleanEditor.Init(
                            booleanProperty.GetName(),
                            booleanProperty.GetGenericValue(), 
                            b => booleanProperty.SetValue(b));

                        _currentEditors.Add(booleanEditor);
                        break;

                    case IntegerProperty integerProperty:
                        var integerEditor = Instantiate(_integerPropertyEditorPrefab, _editorsContainer);
                        integerEditor.Init(
                            integerProperty.GetName(), 
                            integerProperty.GetGenericValue(),
                            i => integerProperty.SetValue(i));
                        _currentEditors.Add(integerEditor);
                        break;

                    case FloatProperty floatProperty:
                        var floatEditor = Instantiate(_floatPropertyEditorPrefab, _editorsContainer);
                        floatEditor.Init(
                            floatProperty.GetName(), 
                            floatProperty.GetGenericValue(),
                            f => floatProperty.SetValue(f));
                        _currentEditors.Add(floatEditor);
                        break;

                    case FloatRangedProperty floatRangedProperty:
                        var floatRangedEditor = Instantiate(_floatRangedPropertyEditorPrefab, _editorsContainer);
                        floatRangedEditor.Init(
                            floatRangedProperty.GetName(), 
                            floatRangedProperty.GetMinValue(), 
                            floatRangedProperty.GetMaxValue(),
                            floatRangedProperty.GetGenericValue(), 
                            f => floatRangedProperty.SetValue(f));
                        _currentEditors.Add(floatRangedEditor);
                        break;

                    case IntegerRangedProperty integerRangedProperty:
                        var integerRangedEditor = Instantiate(_integerRangedPropertyEditorPrefab, _editorsContainer);
                        integerRangedEditor.Init(
                            integerRangedProperty.GetName(), 
                            integerRangedProperty.GetMinValue(), 
                            integerRangedProperty.GetMaxValue(),
                            integerRangedProperty.GetGenericValue(), 
                            i => integerRangedProperty.SetValue(i));
                        _currentEditors.Add(integerRangedEditor);
                        break;

                    case EnumProperty enumProperty:
                        var enumEditor = Instantiate(_enumPropertyEditorPrefab, _editorsContainer);
                        enumEditor.Init(
                            enumProperty.GetName(), 
                            enumProperty.GetEnumValuesNames(), 
                            (int)enumProperty.GetValue(),
                            i => enumProperty.SetValue(i));
                        _currentEditors.Add(enumEditor);
                        break;

                    case StringProperty stringProperty:
                        var stringEditor = Instantiate(_stringPropertyEditorPrefab, _editorsContainer);
                        stringEditor.Init(
                            stringProperty.GetName(),
                            stringProperty.GetGenericValue(),
                            s => stringProperty.SetValue(s));
                        _currentEditors.Add(stringEditor);
                        break;
                }
            }
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void ClearCurrentEditors()
        {
            foreach (var propertyEditor in _currentEditors) Destroy(propertyEditor.gameObject);
            _currentEditors.Clear();
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Groups selection logic

        private List<string> _currentGroupsOptions;

        public string GetCurrentSelectedGroup() => _currentGroupsOptions[_addToGroupDropdown.value];

        private void UpdateGroupsDropdown()
        {
            _currentGroupViews.ForEach(cgv => {if (cgv != null) Destroy(cgv.gameObject);});
            _currentGroupViews.Clear();
            _addToGroupDropdown.ClearOptions();

            if (_inspectedObject == null) return;

            _currentGroupsOptions = _tileEditor
                .CurrentLocation
                .IterateGroups()
                .Where(gr => !_inspectedObject.IsInGroup(gr))
                .OrderBy(gr => gr).ToList();

            _addToGroupDropdown.AddOptions(_currentGroupsOptions);
            _addToGroupButton.gameObject.SetActive(true);

            foreach (string groupName in _inspectedObject.IterateGroups().OrderBy(gr => gr))
            {
                var gv = Instantiate(_objectGroupViewPrefab, _objectGroupViewContainer);
                gv.Init(groupName, () =>
                {
                    _inspectedObject.RemoveFromGroup(groupName);
                    _currentGroupViews.Remove(gv);
                    Destroy(gv.gameObject);
                    UpdateGroupsDropdown();
                });

                _currentGroupViews.Add(gv);
            }
        }

        private void TryAddToGroup()
        {
            _inspectedObject.AddToGroup(GetCurrentSelectedGroup());
            UpdateGroupsDropdown();
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
