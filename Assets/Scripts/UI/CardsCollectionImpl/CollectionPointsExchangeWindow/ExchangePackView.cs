using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class ExchangePackView : MonoBehaviour
    {
        [SerializeField] private Button _buyPackButton;
        [SerializeField] private Button _packInfoButton;
        [SerializeField] private TextMeshProUGUI _starsAmountText;
        [SerializeField] private Image _packImage;
        [SerializeField] private Image _buttonBackground;
        [SerializeField] private Sprite _disabledBackgroundSprite;
        [SerializeField] private Sprite _enabledBackgroundSprite;
        [SerializeField] private RectTransform _rectTransform;

        private ExchangePackEntry _packEntry;
        public event Action<string> OnButtonClicked;
        public event Action<string, RectTransform> OnPackClicked;

        private void Start()
        {
            _buyPackButton.onClick.AddListener(OnBuyButtonClickHandler);
            _packInfoButton.onClick.AddListener(OnInfoButtonClickHandler);
        }

        private void OnBuyButtonClickHandler()
        {
            OnButtonClicked?.Invoke(_packEntry.Id);
        }

        private void OnInfoButtonClickHandler()
        {
            OnPackClicked?.Invoke(_packEntry.Id, _rectTransform);
        }

        public void SetData(int starsAmount, ExchangePackEntry packEntry)
        {
            _packEntry =  packEntry;
            _packImage.sprite = packEntry.Sprite;
            _starsAmountText.text = packEntry.PackPrice.ToString("N0");
            _buttonBackground.sprite = packEntry.PackPrice < starsAmount ? _enabledBackgroundSprite : _disabledBackgroundSprite;
        }
        
        private void OnDestroy()
        {
            _buyPackButton.onClick.RemoveAllListeners();
            _packInfoButton.onClick.RemoveAllListeners();
        }
    }
}