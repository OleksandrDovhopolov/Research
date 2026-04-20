using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rewards
{
    public sealed class RewardedAdButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private GameObject _loadingIndicator;

        public event Action Clicked;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleButtonClicked);
            }
        }

        public void SetInteractable(bool value)
        {
            if (_button != null)
            {
                _button.interactable = value;
            }
        }

        public void SetStatus(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text ?? string.Empty;
            }
        }

        public void SetLoading(bool isVisible)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(isVisible);
            }
        }

        private void HandleButtonClicked()
        {
            Clicked?.Invoke();
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleButtonClicked);
            }
        }
    }
}
