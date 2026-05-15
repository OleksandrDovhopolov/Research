using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public abstract class BasePopup : MonoBehaviour
    {
        private const float _CLOSE_WINDOW_SPEED = 6f;

        [SerializeField] private Text titleText;
        [SerializeField] private Text mainText;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform window;

        private Action<BasePopup> _onCloseAction;

        public void Init(BasePopupModel model, Action<BasePopup> onCloseAction)
        {
            titleText.text = model.title;

            if (mainText != null)
            {
                if (string.IsNullOrEmpty(model.mainText)) mainText.gameObject.SetActive(false);
                else mainText.text = model.mainText;
            }

            closeButton.gameObject.SetActive(model.allowClose);
            closeButton.onClick.AddListener(ClosePopup);

            if (model.width != null) window.sizeDelta = new Vector2(model.width.Value, window.sizeDelta.y);
            _onCloseAction = onCloseAction;
            OnInit(model);
        }

        protected abstract void OnInit(BasePopupModel model);

        public void ClosePopup()
        {
            OnClose();
            StartCoroutine(ClosePopupRoutine());
        }

        private IEnumerator ClosePopupRoutine()
        {
            while (window.localScale.x > 0)
            {
                window.localScale -= Vector3.one * Time.deltaTime * _CLOSE_WINDOW_SPEED;
                yield return null;
            }

            _onCloseAction?.Invoke(this);
            Destroy(gameObject);
        }

        protected virtual void OnClose() { }
    }
}