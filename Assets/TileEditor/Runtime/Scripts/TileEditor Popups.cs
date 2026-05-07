using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fabros.TileEditor
{
    public partial class TileEditor
    {
        //--------------------------------------------------------------------------------------------------------------------------

        [Header("Popups References")]
        [SerializeField] private DialogPopup _dialogPopupPrefab;
        [SerializeField] private InputFieldPopup _inputFieldPopupPrefab;
        [SerializeField] private TopInfoPopup _topInfoPopupPrefab;
        [SerializeField] private SelectObjectPopup _selectObjectPopupPrefab;
        [SerializeField] private Transform _popupsContainer;

        private readonly List<BasePopup> _openedPopups = new List<BasePopup>();

        //--------------------------------------------------------------------------------------------------------------------------

        public bool IsAnyPopupOpened => _openedPopups.Count > 0;

        public BasePopup OpenPopup(BasePopupModel model)
        {
            BasePopup popupPrefab = null;
            switch (model)
            {
                case DialogPopupModel _:
                    popupPrefab = _dialogPopupPrefab;
                    break;
                case InputFieldPopupModel _:
                    popupPrefab = _inputFieldPopupPrefab;
                    break;
                case TopInfoPopupModel _:
                    popupPrefab = _topInfoPopupPrefab;
                    break;
                case SelectObjectPopupModel _:
                    popupPrefab = _selectObjectPopupPrefab;
                    break;
            }

            var popup = Instantiate(popupPrefab, _popupsContainer);
            popup.Init(model, ClosePopupHandler);
            _openedPopups.Add(popup);
            return popup;
        }

        private void ClosePopupHandler(BasePopup popup) => _openedPopups.Remove(popup);

        //--------------------------------------------------------------------------------------------------------------------------

        [Header("Hints")]
        public RectTransform hintContainer;
        public Text hintText;

        private Object _hintOwner;
        private Coroutine _moveHintRoutine;

        public static void ShowHint(string text, Object hintOwner)
        {
            if (_instance == null) return;
            _instance.DoShowHint(text, hintOwner);
        }

        private void DoShowHint(string text, Object hintOwner)
        {
            hintContainer.gameObject.SetActive(true);
            hintText.text = text;
            hintContainer.transform.position = Input.mousePosition;
            _hintOwner = hintOwner;

            if (_moveHintRoutine != null) StopCoroutine(_moveHintRoutine);
            _moveHintRoutine = StartCoroutine(MoveHintRoutine());
        }

        private IEnumerator MoveHintRoutine()
        {
            while (true)
            {
                yield return null;
                if (hintContainer == null) yield break;
                hintContainer.transform.position = Input.mousePosition;

                var hintTopYCoord = hintContainer.anchoredPosition.y + hintText.rectTransform.sizeDelta.y;
                var screenTopYCoord = _canvasRectTransform.rect.yMax;
                if (hintTopYCoord > screenTopYCoord)
                {
                    hintContainer.anchoredPosition += new Vector2(0, screenTopYCoord - hintTopYCoord - 40);
                }
            }
        }

        public static void TryHideHint(Object hintOwner, bool forceHide = false)
        {
            if (_instance == null) return;
            _instance.DoHideHint(hintOwner, forceHide);
        }

        private void DoHideHint(Object hintOwner, bool forceHide)
        {
            if (_hintOwner != hintOwner && !forceHide) return;

            hintContainer.gameObject.SetActive(false);
            if (_moveHintRoutine != null) StopCoroutine(_moveHintRoutine);
            _moveHintRoutine = null;
        }

        public static int GetHintTextSize() => _instance == null ? 0 : _instance.hintText.fontSize;

        //--------------------------------------------------------------------------------------------------------------------------
    }
}