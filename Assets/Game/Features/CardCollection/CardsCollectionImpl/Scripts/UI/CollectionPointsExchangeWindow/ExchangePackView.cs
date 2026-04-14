using System;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
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

        private CardCollectionOfferConfig _packEntry;
        public event Action<string, RectTransform> OnButtonClicked;
        public event Action<string, RectTransform> OnPackClicked;

        private void Start()
        {
            _buyPackButton.onClick.AddListener(OnBuyButtonClickHandler);
            _packInfoButton.onClick.AddListener(OnInfoButtonClickHandler);
        }

        private void OnBuyButtonClickHandler()
        {
            OnButtonClicked?.Invoke(_packEntry.id, _rectTransform);
        }

        private void OnInfoButtonClickHandler()
        {
            OnPackClicked?.Invoke(_packEntry.id, _rectTransform);
        }

        public void SetData(int starsAmount, CardCollectionOfferConfig packEntry)
        {
            _packEntry =  packEntry;
            _starsAmountText.text = packEntry.packPrice.ToString("N0");
            _buttonBackground.sprite = packEntry.packPrice < starsAmount ? _enabledBackgroundSprite : _disabledBackgroundSprite;
            
            LoadSprite().Forget();
        }

        private async UniTask LoadSprite()
        {
            var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(_packEntry.spriteId, this.GetCancellationTokenOnDestroy());
            _packImage.sprite = sprite;
        }
        
        private void OnDestroy()
        {
            ProdAddressablesWrapper.Release(_packEntry?.spriteId);
            _packImage.sprite = null;
            _buyPackButton.onClick.RemoveAllListeners();
            _packInfoButton.onClick.RemoveAllListeners();
        }
    }
}