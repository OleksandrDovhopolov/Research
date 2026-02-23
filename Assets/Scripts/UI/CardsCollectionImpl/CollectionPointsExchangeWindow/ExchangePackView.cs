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

        public event Action OnButtonClicked;

        private void Start()
        {
            _cardsCollectionPointsViewButton.onClick.AddListener(OnButtonClickHandler);
        }

        private void OnButtonClickHandler()
        {
            OnButtonClicked?.Invoke();
        }

        public void SetStarsAmount(float amount)
        {
            _starsAmountText.text = amount.ToString("N0");
        }

        public void SetPackImage(Sprite sprite)
        {
            _packImage.sprite = sprite;
        }
        
        private void OnDestroy()
        {
            _cardsCollectionPointsViewButton.onClick.RemoveAllListeners();
        }
    }
}