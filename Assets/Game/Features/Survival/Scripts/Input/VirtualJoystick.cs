using UnityEngine;
using UnityEngine.EventSystems;

namespace Survival
{
    // Fixed on-screen joystick. Drives both mouse (editor) and touch (mobile)
    // through Unity's EventSystem with no platform branching.
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _handleRange = 75f;
        [SerializeField] private float _deadZone = 0.1f;

        [Header("Quadrant Highlights")]
        [SerializeField] private GameObject _topLeft;
        [SerializeField] private GameObject _topRight;
        [SerializeField] private GameObject _botLeft;
        [SerializeField] private GameObject _botRight;

        private Vector2 _input;
        private Camera _uiCamera;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            _uiCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera
                ? canvas.worldCamera
                : null;

            UpdateQuadrantHighlight();
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background, eventData.position, _uiCamera, out var local);

            Vector2 size = _background.sizeDelta;
            Vector2 normalized = new Vector2(
                local.x / (size.x * 0.5f),
                local.y / (size.y * 0.5f));

            _input = normalized.magnitude > 1f ? normalized.normalized : normalized;
            if (_input.magnitude < _deadZone)
                _input = Vector2.zero;

            _handle.anchoredPosition = _input * _handleRange;
            Publish();
            UpdateQuadrantHighlight();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _input = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
            Publish();
            UpdateQuadrantHighlight();
        }

        private void Publish()
        {
            PlayerInputBridge.JoystickAxis = _input;
            PlayerInputBridge.JoystickActive = true;
        }

        // Highlights the single quadrant the handle currently sits in.
        // All off when the handle is centered (within the dead zone).
        private void UpdateQuadrantHighlight()
        {
            bool active = _input.sqrMagnitude > 0f;
            bool right = _input.x >= 0f;
            bool top = _input.y >= 0f;

            SetHighlight(_topLeft, active && !right && top);
            SetHighlight(_topRight, active && right && top);
            SetHighlight(_botLeft, active && !right && !top);
            SetHighlight(_botRight, active && right && !top);
        }

        private static void SetHighlight(GameObject target, bool on)
        {
            if (target != null && target.activeSelf != on)
                target.SetActive(on);
        }

        private void OnEnable() => PlayerInputBridge.JoystickActive = true;

        private void OnDisable()
        {
            PlayerInputBridge.Reset();
            _input = Vector2.zero;
            UpdateQuadrantHighlight();
        }
    }
}
