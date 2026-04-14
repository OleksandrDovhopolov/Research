using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class GroupCollectionView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _cardsGroupName;
        [SerializeField] private Image _cardsGrouopImage;
        [SerializeField] private Button _cardsGrouopButton;
        
        [Space, Header("Rewards")]
        [SerializeField] private Image _grouoRewardImage;
        [SerializeField] private TextMeshProUGUI _groupRewardAmountText;
        [SerializeField] private CollectedAmountProgressView _collectedAmountProgressView;
        [SerializeField] private Image _groupCompletedImage;
        [SerializeField] private RectTransform _animationStartPosition;
        
        [Space, Header("NewCards")]
        [SerializeField] private GameObject _newCardsContainer;
        //[SerializeField] private TextMeshProUGUI _nexCardsAmountText;

        public string GroupType { get; private set; }
        public Vector3 AnimationStartPosition => _animationStartPosition.transform.position;

        public event Action<string> OnButtonPressed;

        private void Start()
        {
            _cardsGrouopButton.onClick.AddListener(OnButtonPressedHandler);
        }

        private void OnButtonPressedHandler()
        {
            OnButtonPressed?.Invoke(GroupType);
        }
        
        public void SetData(string groupType, string groupName)
        {
            GroupType = groupType;
            _cardsGroupName.text = groupName;
        }

        public void UpdateNewCards(int newCardsAmount)
        {
            _newCardsContainer.SetActive(newCardsAmount > 0);
            //_nexCardsAmountText.text = newCardsAmount.ToString();
        }
        
        public void SetCollectedAmountProgressStart(int collectedAmount, int totalAmount)
        {
            _collectedAmountProgressView.SetPreviousProgress(collectedAmount, totalAmount);
        }
        
        public void UpdateCollectedAmountAnimated(int collectedAmount, int totalAmount, Action<bool> onAnimationCompleted = null)
        {
            _collectedAmountProgressView.UpdateCollectedAmountAnimated(collectedAmount, totalAmount, onAnimationCompleted);
        }

        public void SetGroupCompleted(bool isCompleted)
        {
            _collectedAmountProgressView.gameObject.SetActive(!isCompleted);
            _groupCompletedImage.gameObject.SetActive(isCompleted);
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
        
        public Image GetGroupImage()
        {
            return _cardsGrouopImage;
        }

        public void SetSprite(Sprite groupSprite, bool shouldRelease = false)
        {
            _cardsGrouopImage.sprite = groupSprite;
        }

        public void ShowCompleteCheckMark()
        {
        }
    }
}

