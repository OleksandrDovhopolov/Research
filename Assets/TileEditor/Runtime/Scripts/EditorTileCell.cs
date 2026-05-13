using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fabros.TileEditor
{
    public class EditorTileCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        //------------------------------------------------------------------------------------------------------------------

        private Action<int, int> _onLeftClickAction;
        private Action<int, int> _onMiddleButtonClickAction;
        private Action<int, int, bool> _onHoverAction;

        //------------------------------------------------------------------------------------------------------------------

        [SerializeField] private SpriteRenderer _gridSpriteRenderer;
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private TextMeshPro _gridCoordsText;

        //------------------------------------------------------------------------------------------------------------------

        private readonly Color _defaultColor = new Color(.7f, .7f, .7f, .8f);
        private readonly Color _highlightColor = new Color(0, 1, 1, .8f);

        //------------------------------------------------------------------------------------------------------------------

        public int X { get; private set; }
        public int Y { get; private set; }

        //------------------------------------------------------------------------------------------------------------------

        public void Init(int x, int y, float cellSizeX, float cellSizeY, Action<int, int> onLeftButtonClickAction, Action<int, int> onMiddleButtonClickAction,
            Action<int, int, bool> onHoverAction)
        {
            X = x;
            Y = y;
            _gridSpriteRenderer.size = new Vector2(cellSizeX, cellSizeY);
            _collider.size = new Vector3(cellSizeX, cellSizeY, 0.1f) * 0.95f;
            _onLeftClickAction = onLeftButtonClickAction;
            _onMiddleButtonClickAction = onMiddleButtonClickAction;
            _onHoverAction = onHoverAction;
            _gridCoordsText.text = $"{x}, {y}";
            Highlight(false);
            SetGridCoordsEnabled(false);
        }

        public void Highlight(bool highlightEnabled) =>
            _gridSpriteRenderer.color = highlightEnabled ? _highlightColor : _defaultColor;

        public void SetGridCoordsEnabled(bool isEnabled) => _gridCoordsText.gameObject.SetActive(isEnabled);

        public void SetGridRendererEnabled(bool isEnabled)
        {
            _gridSpriteRenderer.enabled = isEnabled;
            _gridSpriteRenderer.sortingLayerName = isEnabled ? "UI" : "Default";
            _gridSpriteRenderer.sortingOrder = isEnabled ? 1000 : 0;
        }

        //------------------------------------------------------------------------------------------------------------------

        public void OnPointerEnter(PointerEventData eventData)
        {
            Highlight(true);
            _onHoverAction?.Invoke(X, Y, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Highlight(false);
            _onHoverAction?.Invoke(X, Y, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    _onLeftClickAction?.Invoke(X, Y);
                    break;
                case PointerEventData.InputButton.Middle:
                    _onMiddleButtonClickAction?.Invoke(X, Y);
                    break;
            }
        }

        //------------------------------------------------------------------------------------------------------------------
    }
}