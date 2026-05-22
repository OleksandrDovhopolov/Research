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

        private Vector2 _input;
        private Camera _uiCamera;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            _uiCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera
                ? canvas.worldCamera
                : null;
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
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _input = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
            Publish();
        }

        private void Publish()
        {
            PlayerInputBridge.JoystickAxis = _input;
            PlayerInputBridge.JoystickActive = true;
        }

        private void OnEnable() => PlayerInputBridge.JoystickActive = true;

        private void OnDisable() => PlayerInputBridge.Reset();
    }
}
