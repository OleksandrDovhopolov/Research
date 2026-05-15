using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class ObjectReferencePropertyEditor : BasePropertyEditor
    {
        [SerializeField] private SimpleButton _changeObjectButton;
        [SerializeField] private SimpleButton _showSelectedObjectButton;
        [SerializeField] private Text _selectObjectInfo;

        private string _currentObjectReference;
        private LocationObject _currentObject;
        private Action<string> _onValueChangedAction;

        private BasePopup _topInfoPopup;

        public void Init(string fieldName, string value, Action<string> onValueChanged)
        {
            _fieldName.text = fieldName;
            _onValueChangedAction = onValueChanged;
            UpdateSelectedObjectInfo(value);

            _changeObjectButton.Init(ChangeObjectHandler);
            _showSelectedObjectButton.Init(ShowSelectedObjectHandler);
        }

        private void ShowSelectedObjectHandler()
        {
            if (_currentObject != null)
            {
                _currentObject.Highlight();
                TileEditor.GetInstance().MoveCamera(_currentObject.transform.position);
            }
        }

        private void ChangeObjectHandler()
        {
            var tileEditor = TileEditor.GetInstance();

            var topInfoPopupModel = new TopInfoPopupModel();
            topInfoPopupModel.allowClose = true;
            topInfoPopupModel.title = "Select cell";
            topInfoPopupModel.mainText = "Click on cell to select object as reference";
            topInfoPopupModel.onCloseAction += () =>
            {
                tileEditor.SetCustomClickAction(null);
                tileEditor.SetCurrentLocationPanelActive(true);
                tileEditor.SetInspectorPanelActive(true);
            };

            _topInfoPopup = tileEditor.OpenPopup(topInfoPopupModel);

            tileEditor.SetCustomClickAction(SelectObjectsHandler);
            tileEditor.SetCurrentLocationPanelActive(false);
            tileEditor.SetInspectorPanelActive(false);
        }

        private void SelectObjectsHandler(int x, int y)
        {
            var selectObjectsModel = new SelectObjectPopupModel();
            selectObjectsModel.allowClose = true;
            selectObjectsModel.title = "Select object";

            var tileEditor = TileEditor.GetInstance();

            var targetCell = tileEditor.CurrentLocation.GetCell(x, y);

            var availableObjects = targetCell == null
                ? new List<LocationObject>() 
                : targetCell.IterateObjects().ToList();

            selectObjectsModel.mainText = availableObjects.Count == 0
                ? "No objects available on selected cell"
                : "Select object from list";

            selectObjectsModel.availableObjects = availableObjects;
            selectObjectsModel.onObjectSelectedAction = SelectObjectHandler;

            tileEditor.OpenPopup(selectObjectsModel);
        }

        private void SelectObjectHandler(LocationObject obj)
        {
            _topInfoPopup.ClosePopup();
            UpdateSelectedObjectInfo(obj.InstanceId);
            _onValueChangedAction?.Invoke(obj.InstanceId);
        }

        private void UpdateSelectedObjectInfo(string selectedObjectId)
        {
            _currentObjectReference = selectedObjectId;

            if (string.IsNullOrEmpty(selectedObjectId))
            {
                _selectObjectInfo.text = "<i>empty</i>";
                return;
            }

            var obj = TileEditor.GetInstance().CurrentLocation.GetObjectByInstanceId(selectedObjectId);
            if (obj == null)
            {
                _selectObjectInfo.text = $"<i>Bad reference! Old id: {selectedObjectId}</i>";
                _showSelectedObjectButton.gameObject.SetActive(false);
                return;
            }

            _showSelectedObjectButton.gameObject.SetActive(true);
            _currentObject = obj;
            _selectObjectInfo.text = $"<i>{obj.Name} ({obj.Cell.X}, {obj.Cell.Y})</i>";
        }

        public override object GetValue() => _currentObjectReference;
    }
}