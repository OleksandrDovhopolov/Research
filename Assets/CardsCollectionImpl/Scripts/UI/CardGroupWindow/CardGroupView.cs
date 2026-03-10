using System;
using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infrastructure;
using TMPro;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class CardGroupView : WindowView
    {
        [Space, Space, Header("CardsPool")]
        [SerializeField] private UIListPool<CollectionCardView> _upperCardsPool;
        [SerializeField] private UIListPool<CollectionCardView> _bottomCardsPool;
        
        [Space, Space, Header("BaseParts")]
        [SerializeField] private SelectedCardView _selectedCardAnimation;
        [SerializeField] private Button _leftSwitchButton;
        [SerializeField] private Button _rightSwitchButton;
        [SerializeField] private TextMeshProUGUI _collectionNumberText;
        [SerializeField] private TextMeshProUGUI _groupTitleText;
        
        [Space, Space, Header("SlideAnimation")]
        [SerializeField] private RectTransform _cardsContainer;
        [SerializeField] private float _slideDuration = 0.25f;
        [SerializeField] private float _slideOffset = 1200f;
        
        [Space, Space, Header("GroupReward")]
        [SerializeField] private Image _collectedSlider;
        [SerializeField] private TextMeshProUGUI _groupCollectedAmountText;
        [SerializeField] private Image _grouoRewardImage;
        [SerializeField] private TextMeshProUGUI _groupRewardAmountText;

        private readonly Dictionary<CardCollectionConfig, CollectionCardView> _viewsDict = new();
        
        private bool _isAnimating;
        
        public bool IsAnimating => _isAnimating;
        public event Action OnLeftClick;
        public event Action OnRightClick;

        private void Start()
        {
            _leftSwitchButton.onClick.AddListener(() => OnLeftClick?.Invoke());
            _rightSwitchButton.onClick.AddListener(() => OnRightClick?.Invoke());
        }

        public void CreateDataViews(string groupType, List<CardProgressData> cardsData, CardCollectionNewCardsDto newCardsData)
        {
            var configs = CardCollectionConfigStorage.Instance.Get(groupType);
            
            _upperCardsPool.DisableNonActive();
            _bottomCardsPool.DisableNonActive();
            
            _groupTitleText.text = groupType;
            
            for (var i = 0; i < cardsData.Count; i++)
            {
                if (i >= 10)
                {
                    Debug.LogError($"More than 10 elements");
                    break;
                }
                
                var data = cardsData[i];
                var config = configs.Find(config => config.Id == data.CardId);
                if (config == null)
                {
                    Debug.LogError($"Debug. GroupId {groupType}. failed to Find config.ID ==  data.CardId {data.CardId}");
                    continue;
                }
                var pool = i < 5 ? _upperCardsPool : _bottomCardsPool;
        
                var cardView = pool.GetNext();
                cardView.SetConfig(config);
                cardView.SetCardOpen(data.IsUnlocked);
                cardView.SetCardNew(newCardsData.IsNew(data.CardId));
                cardView.UpdateCardFrame();
                cardView.UpdateCardName();
                cardView.UpdateCardStars();
                cardView.OnCardPressed += OnCardPressedHandler;
        
                _viewsDict[config] = cardView;
            }
        }

        public void SetCollectionNumber(string collectionNumber)
        {
            _collectionNumberText.text = collectionNumber;
        }

        public void UpdateCollectedAmount(int collectedAmount, int totalAmount)
        {
            _collectedSlider.fillAmount = (float)collectedAmount / totalAmount;;
            _groupCollectedAmountText.text = collectedAmount.ToString();
        }
        
        public void SetRewardData(Sprite sprite, int amount)
        {
            _grouoRewardImage.sprite = sprite;
            _groupRewardAmountText.text = amount.ToString();
        }
        
        public async UniTask SetSprites(List<CardCollectionConfig> cardsData)
        {
            await UIUtils.LoadAndSetSpritesAsync(
                cardsData,
                config => config.Icon,
                config => _viewsDict.TryGetValue(config, out var view) ? view : null,
                (view, sprite) => view.SetCardImage(sprite),
                config => config.CardName);
        }

        /// <summary>
        /// Slides current group out, rebuilds cards for new group, slides new group in.
        /// direction: -1 = left (show previous group), +1 = right (show next group)
        /// </summary>
        public async UniTask AnimateSwitchGroup(
            int direction,
            string groupType,
            List<CardProgressData> cardsData,
            CardCollectionNewCardsDto newCardsData,
            Action onRebuild = null)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            try
            {
                // Phase 1: Slide current cards container off-screen
                var slideOutX = -direction * _slideOffset;
                await _cardsContainer.DOAnchorPosX(slideOutX, _slideDuration)
                    .SetEase(Ease.InQuad)
                    .AsyncWaitForCompletion()
                    .AsUniTask();

                // Phase 2: Rebuild cards and update UI while container is off-screen
                DisableAll();
                CreateDataViews(groupType, cardsData, newCardsData);
                onRebuild?.Invoke();

                // Phase 3: Snap container to opposite side (off-screen)
                _cardsContainer.anchoredPosition = new Vector2(direction * _slideOffset, 0);

                // Phase 4: Slide new cards into center
                await _cardsContainer.DOAnchorPosX(0, _slideDuration)
                    .SetEase(Ease.OutQuad)
                    .AsyncWaitForCompletion()
                    .AsUniTask();
            }
            finally
            {
                _isAnimating = false;
            }
        }

        private void OnCardPressedHandler(CollectionCardView cardView)
        {
            CardCollectionConfig config = null;
            foreach (var pair in _viewsDict)
            {
                if (ReferenceEquals(pair.Value, cardView))
                {
                    config = pair.Key;
                }
            }

            if (config == null) return;

            _selectedCardAnimation.OnCardPressedHandler(cardView, config);
        }

        public void DisableAll()
        {
            foreach (var cardView in _viewsDict.Values)
            {
                cardView.OnCardPressed -= OnCardPressedHandler;
            }
            _viewsDict.Clear();
            
            _upperCardsPool.DisableAll();
            _bottomCardsPool.DisableAll();
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _leftSwitchButton.onClick.RemoveAllListeners();
            _rightSwitchButton.onClick.RemoveAllListeners();
        }
    }
}