using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class ExchangePackView : MonoBehaviour
    {
        [SerializeField] private Button _cardsCollectionPointsViewButton;
        [SerializeField] private TextMeshProUGUI _starsAmountText;
        [SerializeField] private Image _packImage;
        [SerializeField] private Image _buttonBackground;
        [SerializeField] private Sprite _disabledBackgroundSprite;
        [SerializeField] private Sprite _enabledBackgroundSprite;

        private ExchangePackEntry _packEntry;
        public event Action<string> OnButtonClicked;

        private void Start()
        {
            _cardsCollectionPointsViewButton.onClick.AddListener(OnButtonClickHandler);
        }

        private void OnButtonClickHandler()
        {
            OnButtonClicked?.Invoke(_packEntry.Id);
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
            _cardsCollectionPointsViewButton.onClick.RemoveAllListeners();
        }
    }
}