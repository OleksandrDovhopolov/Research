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
        
        [Space, Header("Rewards")]
        [SerializeField] private Image _grouoRewardImage;
        [SerializeField] private TextMeshProUGUI _groupRewardAmountText;

        private string _groupType;
        
        public event Action<string> OnButtonPressed;

        private void Start()
        {
            _cardsGrouopButton.onClick.AddListener(OnButtonPressedHandler);
        }

        private void OnButtonPressedHandler()
        {
            OnButtonPressed?.Invoke(_groupType);
        }
        
        public void SetData(string groupType, string groupName, string collectedAmount, Sprite groupSprite)
        {
            _groupType = groupType;
            _cardsGroupName.text = groupName;
            _grouoCollectedAmountText.text = collectedAmount;
            _cardsGrouopImage.sprite = groupSprite;
        }

        private void OnDestroy()
        {
            _cardsGrouopButton.onClick.RemoveAllListeners();
        }

        public void UpdateSprite(Sprite groupSprite)
        {
            _cardsGrouopImage.sprite = groupSprite;
        }
    }
}

