using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TileEditor
{
    public class SimpleButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        [SerializeField] private Button _button;
        [SerializeField] private Text _buttonText;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public string ButtonText { get; private set; }
        private string _hintText;
        private Func<string> _getHintText;
        private Action _buttonAction;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void Init(Action buttonAction)
        {
            _buttonAction = buttonAction;
            _button.onClick.AddListener(buttonAction.Invoke);
        }

        public void Init(string buttonText, Action buttonAction)
        {
            _buttonAction = buttonAction;
            ButtonText = buttonText;

            _button.onClick.AddListener(buttonAction.Invoke);

            if (string.IsNullOrEmpty(buttonText)) _buttonText.gameObject.SetActive(false);
            else _buttonText.text = buttonText;
        }

        public void SetText(string newText)
        {
            ButtonText = newText;
            _buttonText.text = newText;
        }

        public void ExecuteAction() => _buttonAction?.Invoke();

        public void SetInteractable(bool isInteractable) => _button.interactable = isInteractable;

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------

        private bool _isHintShown;

        public bool IsHintAvailable => !string.IsNullOrEmpty(_hintText) || _getHintText != null;

        private Coroutine _hintDelayRoutine;
        public void SetGetHintTextFunction(Func<string> func) => _getHintText = func;
        public void SetHintText(string text) => _hintText = text;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || !IsHintAvailable) return;
            ShowHint();
        }

        private void ShowHint()
        {
            if (!gameObject.activeInHierarchy) return;

            TileEditor.ShowHint(_getHintText?.Invoke() ?? _hintText, this);
            _isHintShown = true;

            DestroyPointerTween();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DestroyPointerTween();

            if (!_isHintShown) return;

            _isHintShown = false;
            TileEditor.TryHideHint(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsHintAvailable) return;

            _hintDelayRoutine = StartCoroutine(HintDelayRoutine());
        }

        private IEnumerator HintDelayRoutine()
        {
            yield return new WaitForSeconds(1);
            ShowHint();
        }

        protected void DestroyPointerTween()
        {
            if (_hintDelayRoutine == null) return;

            StopCoroutine(_hintDelayRoutine);
            _hintDelayRoutine = null;
        }

        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}