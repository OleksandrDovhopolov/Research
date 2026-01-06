using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CollectionCardView : MonoBehaviour
    {
        [SerializeField] private Button _cardButton;
        [SerializeField] private TextMeshProUGUI _closedCardName;
        [SerializeField] private TextMeshProUGUI _openCardName;
        [SerializeField] private Image _cardImage;
        
        [SerializeField] private GameObject star1;
        [SerializeField] private GameObject star2;
        [SerializeField] private GameObject star3;
        [SerializeField] private GameObject star4;
        [SerializeField] private GameObject star5;
        
        [SerializeField] protected GameObject _closedCardContainer;
        [SerializeField] protected GameObject _openCardContainer;

        private bool _isOpen;
        private RectTransform _rectTransformOpen;
        private RectTransform _rectTransformClosed;

        public event Action<CollectionCardView> OnCardPressed;

        protected virtual void Awake()
        {
            _rectTransformOpen = _openCardContainer.GetComponent<RectTransform>();
            _rectTransformClosed = _closedCardContainer.GetComponent<RectTransform>();
        }

        public RectTransform GetRectTransform(bool isOpen)
        {
            return isOpen ? _rectTransformOpen : _rectTransformClosed;
        }
        
        private void Start()
        {
            if (_cardButton != null)
            { 
                _cardButton.onClick.AddListener(OnCardPressedHandler);
            }
        }

        private void OnCardPressedHandler()
        {
            OnCardPressed?.Invoke(this);
        }
        
        public void SetCardIsOpen(bool isOpen)
        {
            _isOpen = isOpen;
            UpdateCardView();
        }
                
        public void SetCardName(string cardName)
        {
            _closedCardName.text = cardName;
            _openCardName.text = cardName;
        }
        
        public void SetStars(int starsCount)
        {
            starsCount = Mathf.Clamp(starsCount, 1, 5);
            
            star1.SetActive(false);
            star2.SetActive(false);
            star3.SetActive(false);
            star4.SetActive(false);
            star5.SetActive(false);

            if (starsCount == 5)
            {
                star5.SetActive(true);
            }
            else
            {
                if (starsCount >= 1) star1.SetActive(true);
                if (starsCount >= 2) star2.SetActive(true);
                if (starsCount >= 3) star3.SetActive(true);
                if (starsCount >= 4) star4.SetActive(true);
            }            
        }
        
        public void SetCardImage(Sprite cardSprite)
        {
            _cardImage.sprite = cardSprite;
        }
        
        public void UpdateCardView()
        {
            SetOpenCardContainerActive(_isOpen);
        }
        
        public void SetOpenCardContainerActive(bool isActive)
        {
            _openCardContainer.SetActive(isActive);
        }
        
        public void SetClosedCardContainerActive(bool isActive)
        {
            _closedCardContainer.SetActive(isActive);
        }

        private void OnDestroy()
        {
            if (_cardButton != null)
            {
                _cardButton.onClick.RemoveAllListeners();
            }
        }

        public void OnCardAnimationStarted()
        {
            SetOpenCardContainerActive(false);
        }

        public void OnCardAnimationCompleted()
        {
            SetOpenCardContainerActive(_isOpen);
        }
    }
}