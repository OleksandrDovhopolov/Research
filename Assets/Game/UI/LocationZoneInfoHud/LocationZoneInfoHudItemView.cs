using Game.Features.Locations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared
{
    public sealed class LocationZoneInfoHudItemView : MonoBehaviour
    {
        private const float Width = 120f;
        private const float Height = 48f;
        private static readonly Vector2 LabelPadding = new(12f, 6f);

        private ILocationInteractable _interactable;
        private Transform _target;
        private Button _button;
        private RectTransform _rectTransform;
        private Canvas _parentCanvas;

        public void Initialize(
            ILocationInteractable interactable,
            string label,
            Canvas parentCanvas,
            Sprite backgroundSprite,
            TMP_FontAsset fontAsset)
        {
            _interactable = interactable;
            _target = interactable.HudAnchor;
            _parentCanvas = parentCanvas;
            _rectTransform = gameObject.AddComponent<RectTransform>();
            _rectTransform.sizeDelta = new Vector2(Width, Height);

            BuildButton(label, backgroundSprite, fontAsset);
            UpdateTransform();
        }

        private void LateUpdate()
        {
            UpdateTransform();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        private void BuildButton(string label, Sprite backgroundSprite, TMP_FontAsset fontAsset)
        {
            var image = gameObject.AddComponent<Image>();
            image.sprite = backgroundSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color32(31, 41, 55, 220);
            image.raycastTarget = true;

            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = image;
            _button.onClick.AddListener(HandleClicked);

            var colors = _button.colors;
            colors.normalColor = new Color32(255, 255, 255, 255);
            colors.highlightedColor = new Color32(230, 230, 230, 255);
            colors.pressedColor = new Color32(200, 200, 200, 255);
            colors.selectedColor = colors.normalColor;
            _button.colors = colors;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);

            var labelTransform = (RectTransform)labelObject.transform;
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = LabelPadding;
            labelTransform.offsetMax = -LabelPadding;

            var text = labelObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 24f;
            text.color = Color.white;
            text.raycastTarget = false;

            if (fontAsset != null)
                text.font = fontAsset;
        }

        private void UpdateTransform()
        {
            if (_target == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            transform.position = _target.position;

            var targetCamera = ResolveCamera();
            if (targetCamera != null)
                transform.rotation = targetCamera.transform.rotation;
        }

        private Camera ResolveCamera()
        {
            var mainCamera = Camera.main;

            if (_parentCanvas != null)
            {
                var canvasCamera = _parentCanvas.worldCamera;
                if (canvasCamera != null && canvasCamera.isActiveAndEnabled)
                    return canvasCamera;

                if (mainCamera != null && mainCamera.isActiveAndEnabled)
                    _parentCanvas.worldCamera = mainCamera;
            }

            return mainCamera;
        }

        private void HandleClicked()
        {
            var anchorName = _target != null ? _target.name : string.Empty;
            Debug.Log($"[ZoneInfoHud] Click key='{_interactable?.InteractionKey}', id='{_interactable?.InteractionId}', anchor='{anchorName}'.");
        }
    }
}
