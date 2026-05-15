using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class ObjectsSubCategory : MonoBehaviour
    {
        [SerializeField] private Text _subcategoryName;
        [SerializeField] private Transform _objectsContainer;
        [SerializeField] private Button _collapseSubCategoryButton;

        public string SubCategoryName { get; private set; }

        private TileEditor _tileEditor;
        private readonly Dictionary<string, ToolButton> _categoryToolsButtons = new Dictionary<string, ToolButton>();

        public void Init(string subCatecoryName, IEnumerable<LocationObject> objects, TileEditor tileEditor)
        {
            _tileEditor = tileEditor;
            SubCategoryName = subCatecoryName;

            if (string.IsNullOrEmpty(subCatecoryName))
            {
                _subcategoryName.gameObject.SetActive(false);
                _objectsContainer.gameObject.SetActive(true);
            }
            else
            {
                _subcategoryName.text = $"  {subCatecoryName} [+]";
                _objectsContainer.gameObject.SetActive(false);
                _collapseSubCategoryButton.onClick.AddListener(ToggleCategoryCollapse);
            }

            foreach (LocationObject locationObject in objects.OrderBy(obj => obj.Name)) 
                AddObjectTool(locationObject);
        }

        private void ToggleCategoryCollapse()
        {
            if (_objectsContainer.gameObject.activeSelf) Collapse();
            else Expand();
        }

        public void Expand()
        {
            if (string.IsNullOrEmpty(SubCategoryName)) return;

            _objectsContainer.gameObject.SetActive(true);
            _subcategoryName.text = $"  {SubCategoryName} [-]";
        }

        public void Collapse()
        {
            if (string.IsNullOrEmpty(SubCategoryName)) return;

            _objectsContainer.gameObject.SetActive(false);
            _subcategoryName.text = $"  {SubCategoryName} [+]";
        }

        public void ApplyFilter(string filterString)
        {
            if (string.IsNullOrEmpty(filterString))
            {
                foreach (var keyValuePair in _categoryToolsButtons) 
                    keyValuePair.Value.gameObject.SetActive(true);

                return;
            }

            foreach (var keyValuePair in _categoryToolsButtons)
            {
                var contains = keyValuePair.Key.IndexOf(filterString, StringComparison.OrdinalIgnoreCase) >= 0;
                keyValuePair.Value.gameObject.SetActive(contains);
            }
        }

        private void AddObjectTool(LocationObject obj)
        {
            var objectTool = _tileEditor.CreateToolButton(
                _objectsContainer, 
                obj.GetPreviewSprite(),
                obj.Name,
                obj.Description,
                (x, y) => _tileEditor.AddObject(x, y),
                (x, y, hovered) => _tileEditor.UpdateBrush(x, y, hovered),
                TileEditor.ToolKind.AddObject);

            _tileEditor.RegisterAddObjectButton(obj, objectTool);

            objectTool.OnSelected += () => _tileEditor.ObjectToolSelectedHandler(obj);
            objectTool.OnDeselected += () => _tileEditor.ObjectToolDeselectedHandler(obj);

            _categoryToolsButtons[obj.Name] = objectTool;
        }
    }
}