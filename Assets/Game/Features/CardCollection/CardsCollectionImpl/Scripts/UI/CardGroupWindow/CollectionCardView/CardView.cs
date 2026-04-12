using System;
using CardCollection.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class CardView : MonoBehaviour
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
        [SerializeField] protected GameObject _cardRect;
        
        [SerializeField] private GameObject _newNotificationGameObject;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Space, Header("Frame")]
        [SerializeField] private CardStarVisualCustomizer _cardStarVisualCustomizer;
        
        [Space, Header("FX")]
        [SerializeField] private ParticleSystem _newCardFX;

        private CardConfig _cardCollectionConfig;
        public bool IsOpen { get; private set; }
        public bool IsNew { get; private set; }
        public int Stars { get; private set; }
        public Image CardImage => _cardImage;

        protected RectTransform CardRect;
        
        public event Action<CardView> OnCardPressed;
        
        protected virtual void Awake()
        {
            CardRect = (RectTransform)_cardRect.transform;
        }

        public virtual RectTransform GetRectTransform()
        {
            return CardRect;
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
        
        public void SetConfig(CardConfig config)
        {
            _cardCollectionConfig = config;
        }
        
        public void SetCardOpen(bool isOpen)
        {
            IsOpen = isOpen;
            UpdateCardView();
        }
                
        public void UpdateCardName()
        {
            _closedCardName.text = _cardCollectionConfig.cardName;
            _openCardName.text = _cardCollectionConfig.cardName;
        }

        public void UpdateCardFrame()
        {
            _cardStarVisualCustomizer.ApplyStarTier(Stars, _cardCollectionConfig.premiumCard);
        }

        public void SetNewCardFXActive(bool isActive)
        {
            _newCardFX.gameObject.SetActive(isActive);
        }
        
        public void UpdateCardStars()
        {
            Stars = Mathf.Clamp(_cardCollectionConfig.stars, 1, 5);
            
            star1.SetActive(false);
            star2.SetActive(false);
            star3.SetActive(false);
            star4.SetActive(false);
            star5.SetActive(false);

            if (Stars >= 1) star1.SetActive(true);
            if (Stars >= 2) star2.SetActive(true);
            if (Stars >= 3) star3.SetActive(true);
            if (Stars >= 4) star4.SetActive(true);
            if (Stars >= 5) star5.SetActive(true);
        }
        
        public void SetCardImage(Sprite cardSprite,  bool shouldRelease = false)
        {
            _cardImage.sprite = cardSprite;
        }

        public Image GetCardImage()
        {
            return _cardImage;
        }
        
        public void UpdateCardView()
        {
            SetOpenCardContainerActive(IsOpen);
        }
        
        public void SetOpenCardContainerActive(bool isActive)
        {
            _openCardContainer.SetActive(isActive);
        }
        
        public void SetAlpha(bool isActive)
        {
            _canvasGroup.alpha = isActive ? 1 : 0;
        }
        
        public void SetClosedCardContainerActive(bool isActive)
        {
            _closedCardContainer.SetActive(isActive);
        }

        public void SetCardNew(bool isNew)
        {
            IsNew = isNew;
            _newNotificationGameObject.SetActive(isNew);
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
            SetOpenCardContainerActive(IsOpen);
        }

        public Tween Hide(float delay = 0f, float duration = 0.35f)
        {
            transform.DOKill();
            return transform.DOScale(Vector3.zero, duration)
                .SetDelay(delay)
                .SetEase(Ease.InBack);
        }

        public void ResetView()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
        }
    }
}