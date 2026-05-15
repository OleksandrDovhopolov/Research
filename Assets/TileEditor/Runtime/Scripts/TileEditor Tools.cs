using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public partial class TileEditor
    {
        //--------------------------------------------------------------------------------------------------------------------------

        public enum ToolKind
        {
            None,
            Default,
            AddObject,
            InspectLocation,
            InspectGroups,
            InspectObjects,
            InspectLayers,
            Custom
        }

        //--------------------------------------------------------------------------------------------------------------------------

        [Header("Tools")] 
        [SerializeField] private SimpleButton _simpleButtonPrefab;
        [SerializeField] private ToolButton _toolButtonPrefab;
        [SerializeField] private LayoutElement _delimiterPrefab;

        //--------------------------------------------------------------------------------------------------------------------------

        private readonly List<ToolButton> _toolButtons = new List<ToolButton>();
        private readonly List<SimpleButton> _simpleButtons = new List<SimpleButton>();

        //--------------------------------------------------------------------------------------------------------------------------

        public static event Action<ToolButton> OnToolChanged;

        private ToolButton _currentToolButton;

        public ToolButton CurrentToolButton
        {
            get => _currentToolButton;
            private set
            {
                if (_currentToolButton == value)
                {
                    if (value != null) CurrentToolButton = null;
                    return;
                }

                CurrentToolKind = value == null ? ToolKind.None : value.Kind;

                if (_currentToolButton != null) _currentToolButton.DeselectTool();
                _currentToolButton = value;
                OnToolChanged?.Invoke(value);
            }
        }

        private ToolKind _currentToolKind;

        public ToolKind CurrentToolKind
        {
            get => _currentToolKind;
            private set
            {
                if (_currentToolKind == value) return;
                UpdateCurrentToolKind(_currentToolKind, value);
                _currentToolKind = value;
            }
        }

        private void UpdateCurrentToolKind(ToolKind oldToolKind, ToolKind newToolKind)
        {
            switch (oldToolKind)
            {
                case ToolKind.None: break;
                case ToolKind.Default: break;
                case ToolKind.InspectLocation: break;
                case ToolKind.InspectGroups: break;
                case ToolKind.InspectLayers: break;
                case ToolKind.AddObject: break;
                
                case ToolKind.InspectObjects:
                    InspectObjectsList(null);
                    _objectsListInspector.gameObject.SetActive(false);
                    break;

                case ToolKind.Custom: break;

                default: throw new ArgumentOutOfRangeException(nameof(oldToolKind), oldToolKind, null);
            }

            switch (newToolKind)
            {
                case ToolKind.None: break;
                case ToolKind.Default: break;
                case ToolKind.InspectLocation: break;
                case ToolKind.InspectGroups: break;
                case ToolKind.AddObject: break;
                case ToolKind.InspectLayers: break;

                case ToolKind.InspectObjects: 
                    _objectsListInspector.gameObject.SetActive(true);
                    break;

                case ToolKind.Custom: break;

                default: throw new ArgumentOutOfRangeException(nameof(newToolKind), newToolKind, null);
            }
        }

        public void DeselectCurrentTool()
        {
            TryHideHint(null, true);
            CurrentToolButton = null;
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public void AddCustomButton(string buttonText, string buttonHintText, Action onClickAction)
        {
            _activeLocationPanel.AddCustomButton(buttonText, buttonHintText, onClickAction);
        }

        public void AddCustomToolButton(string buttonText, string buttonHintText, Action<int, int> buttonAction, Action<int, int, bool> buttonHoverAction, Action onToolActivatedAction, Action onToolDeactivatedAction, bool allowDragInput)
        {
            _activeLocationPanel.AddCustomToolButton(buttonText, buttonHintText, buttonAction, buttonHoverAction, onToolActivatedAction, onToolDeactivatedAction, allowDragInput);
        }

        //--------------------------------------------------------------------------------------------------------------------------

        public SimpleButton CreateButton(Transform container, string buttonText, Action buttonAction)
        {
            var newButton = Instantiate(_simpleButtonPrefab, container);
            newButton.Init(buttonText, buttonAction);

            _simpleButtons.Add(newButton);
            return newButton;
        }

        public ToolButton CreateToolButton(Transform container, string buttonText, Action<int, int> buttonAction,
            Action<int, int, bool> toolHoverAction,
            ToolKind toolKind = ToolKind.Default, bool allowDragInput = true)
        {
            var newButton = Instantiate(_toolButtonPrefab, container);
            newButton.InitToolButton(buttonText, buttonAction, toolHoverAction, ToolSelectedHandler, toolKind, allowDragInput);

            _toolButtons.Add(newButton);
            return newButton;
        }

        public ToolButton CreateToolButton(Transform container, Sprite sprite, string buttonName, string buttonDescription,
            Action<int, int> buttonAction,
            Action<int, int, bool> toolHoverAction,
            ToolKind toolKind = ToolKind.Default, bool allowDragInput = true)
        {
            var newButton = Instantiate(_toolButtonPrefab, container);
            newButton.InitToolButton(sprite, buttonAction, toolHoverAction, ToolSelectedHandler, toolKind, allowDragInput);
            var hintTextStr = buttonName + (string.IsNullOrEmpty(buttonDescription) ? "" : $"\n\n{buttonDescription}");
            newButton.SetHintText(hintTextStr);
            _toolButtons.Add(newButton);
            return newButton;
        }

        public void CreateDelimiter(Transform container, float height)
        {
            var delimiter = Instantiate(_delimiterPrefab, container);
            delimiter.preferredHeight = height;
            var rectTransform = delimiter.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
        }

        //--------------------------------------------------------------------------------------------------------------------------

        private void ToolSelectedHandler(ToolButton selectedTool)
        {
            CurrentToolButton = selectedTool;
        }

        private void CellLeftClickHandler(int x, int y)
        {
            if (_customClickAction != null)
            {
                _customClickAction.Invoke(x, y);
                return;
            }

            if (CurrentToolButton != null) CurrentToolButton.InvokeToolAction(x, y);
        }

        private void CellMiddleClickHandler(int x, int y)
        {
            // smart delete
            var deleteObject = CurrentLocation.GetCell(x, y)?.IterateObjects().FirstOrDefault();
            if (deleteObject != null)
                ExecuteCommand(new RemoveObjectCommand(CurrentLocation, deleteObject));
        }

        private void CellHoverHandler(int x, int y, bool isHovered)
        {
            CurrentToolButton?.InvokeToolCellHoverAction(x, y, isHovered);
        }

        private void ToggleTool(ToolKind toolKind)
        {
            if (CurrentToolButton == null || CurrentToolButton.Kind != toolKind)
                _toolButtons.Find(tb => tb.Kind == toolKind).SelectTool();
            else 
                CurrentToolButton = null;
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
}