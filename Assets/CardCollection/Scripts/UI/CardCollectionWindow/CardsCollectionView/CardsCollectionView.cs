using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core
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

        public void UpdateCollectedAmount(int collectedAmount, int totalAmount)
        {
            _collectedSlider.fillAmount = (float)collectedAmount / totalAmount;;
            _grouoCollectedAmountText.text = collectedAmount.ToString();
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

