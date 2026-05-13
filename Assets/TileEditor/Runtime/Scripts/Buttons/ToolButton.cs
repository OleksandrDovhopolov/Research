using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public class ToolButton : SimpleButton
    {
        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private static readonly Color _defalutColor = Color.white;
        private static readonly Color _selectedColor = Color.yellow;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        [SerializeField] private Image _backroundImage;
        [SerializeField] private Image _mainImage;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private Action<int, int> _toolAction;
        private Action<int, int, bool> _toolCellHoverAction;
        private Action<ToolButton> _onToolSelectedAction;

        public TileEditor.ToolKind Kind { get; private set; }
        public bool AllowDragInput { get; set; }

        public event Action OnSelected;
        public event Action OnDeselected;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void InitToolButton(string buttonText, Action<int, int> buttonAction, Action<int, int, bool> toolHoverAction, Action<ToolButton> onToolSelected,
            TileEditor.ToolKind toolKind, bool allowDragInput)
        {
            DoInit(buttonText, buttonAction, toolHoverAction, onToolSelected, toolKind, allowDragInput);

            _mainImage.gameObject.SetActive(false);
        }

        public void InitToolButton(Sprite sprite, Action<int, int> buttonAction, Action<int, int, bool> toolHoverAction, Action<ToolButton> onToolSelected,
            TileEditor.ToolKind toolKind, bool allowDragInput)
        {
            DoInit(null, buttonAction, toolHoverAction, onToolSelected, toolKind, allowDragInput);

            _mainImage.gameObject.SetActive(true);
            _mainImage.sprite = sprite;
        }

        private void DoInit(string buttonText, Action<int, int> buttonAction, Action<int, int, bool> toolHoverAction, Action<ToolButton> onToolSelected,
            TileEditor.ToolKind toolKind, bool allowDragInput)
        {
            Init(buttonText, SelectTool);
            _toolAction = buttonAction;
            _toolCellHoverAction = toolHoverAction;
            _onToolSelectedAction = onToolSelected;
            Kind = toolKind;
            AllowDragInput = allowDragInput;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void DeselectTool()
        {
            OnDeselected?.Invoke();
            _backroundImage.color = _defalutColor;

            DestroyPointerTween();
            TileEditor.TryHideHint(this);
        }

        public void SelectTool()
        {
            OnSelected?.Invoke();
            _backroundImage.color = _selectedColor;
            _onToolSelectedAction?.Invoke(this);
        }

        public void InvokeToolAction(int x, int y) => _toolAction?.Invoke(x, y);
        public void InvokeToolCellHoverAction(int x, int y, bool isHovered) => _toolCellHoverAction?.Invoke(x, y, isHovered);

        public Image GetMainImage() => _mainImage;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}