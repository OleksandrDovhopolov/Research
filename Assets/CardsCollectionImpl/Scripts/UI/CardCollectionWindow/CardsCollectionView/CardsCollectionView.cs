using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class CardsCollectionView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _cardsGroupName;
        [SerializeField] private Image _cardsGrouopImage;
        [SerializeField] private Button _cardsGrouopButton;
        [SerializeField] private TextMeshProUGUI _grouoCollectedAmountText;
        [SerializeField] private Image _collectedSlider;
        
        [Space, Header("Rewards")]
        [SerializeField] private Image _grouoRewardImage;
        [SerializeField] private TextMeshProUGUI _groupRewardAmountText;
        
        [Space, Header("NewCards")]
        [SerializeField] private GameObject _newCardsContainer;
        [SerializeField] private TextMeshProUGUI _nexCardsAmountText;

        public string GroupType { get; private set; }

        public event Action<string> OnButtonPressed;

        private void Start()
        {
            _cardsGrouopButton.onClick.AddListener(OnButtonPressedHandler);
        }

        private void OnButtonPressedHandler()
        {
            OnButtonPressed?.Invoke(GroupType);
        }
        
        public void SetData(string groupType, string groupName, int collectedAmount, int totalAmount)
        {
            GroupType = groupType;
            _cardsGroupName.text = groupName;
            UpdateCollectedAmount(collectedAmount, totalAmount);
        }

        public void UpdateNewCards(int newCardsAmount)
        {
            _newCardsContainer.SetActive(newCardsAmount > 0);
            _nexCardsAmountText.text = newCardsAmount.ToString();
        }
        
        public void UpdateCollectedAmount(int collectedAmount, int totalAmount)
        {
            _collectedSlider.fillAmount = (float)collectedAmount / totalAmount;;
            _grouoCollectedAmountText.text = collectedAmount.ToString();
        }

        public void SetRewardData(Sprite sprite, int amount)
        {
            _grouoRewardImage.sprite = sprite;
            _groupRewardAmountText.text = amount.ToString();
        }
        
        private void OnDestroy()
        {
            _cardsGrouopButton.onClick.RemoveAllListeners();
        }

        public void SetSprite(Sprite groupSprite)
        {
            _cardsGrouopImage.sprite = groupSprite;
        }
    }
}

