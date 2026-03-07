using System;
using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using TMPro;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class CardCollectionView : WindowView
    {
        [SerializeField] private UIListPool<CardsCollectionView> _cardGroupsPool;
        [SerializeField] private GameObject _loadingAnimationObject;
        [SerializeField] private CardCollectionRewardsConfigSO _cardCollectionRewardsConfigSo;
        [SerializeField] private Button _infoButton;
        
        [Header("Points Container")]
        [SerializeField] private CardsCollectionPointsView _cardsCollectionPointsView;
        
        [Space, Space, Header("GroupReward")]
        [SerializeField] private CollectedAmountProgressView _collectedAmountProgressView;
        [SerializeField] private Button _collectionRewardButton;
        [SerializeField] private RectTransform _collectionRewardButtonRect;
        
        [Header("Points Container")]
        [SerializeField] private TextMeshProUGUI _timerText;
        
        private readonly Dictionary<string, CardsCollectionView> _viewsDict = new();

        public CardCollectionRewardsConfigSO RewardsConfigSo => _cardCollectionRewardsConfigSo;
        
        public event Action<string> OnGroupButtonPressed;
        public event Action OnPointsViewClicked;
        public event Action OnInfoButtonClicked;
        public event Action<RectTransform> OnRewardChestClicked;
        
        private bool _groupsCreated;

        private void Start()
        {
            _cardsCollectionPointsView.OnViewClicked += OnPointsViewClickedHandler;
            _collectionRewardButton.onClick.AddListener(OnRewardChestClickedHandler);
            _infoButton.onClick.AddListener(OnInfoButtonClickedHandler);
        }

        private void OnPointsViewClickedHandler()
        {
            OnPointsViewClicked?.Invoke();
        }
        
        private void OnRewardChestClickedHandler()
        {
            OnRewardChestClicked?.Invoke(_collectionRewardButtonRect);
        }
        
        private void OnInfoButtonClickedHandler()
        {
            OnInfoButtonClicked?.Invoke();
        }
        
        public void CreateViews(CardCollectionNewCardsDto newCardsData)
        {
            _cardGroupsPool.DisableNonActive();

            _viewsDict.Clear();

            var configs = CardGroupsConfigStorage.Instance.Data;
            
            foreach (var groupsConfig in configs)
            {
                var groupView = _cardGroupsPool.GetNext();
                
                var groupType = groupsConfig.GroupType;
                var groupName = groupsConfig.GroupName;
                
                groupView.SetData(groupType, groupName);
                var rewardViewData = UIUtils.CreateRewardViewData(_cardCollectionRewardsConfigSo, groupType);
                groupView.SetRewardData(rewardViewData.Icon, rewardViewData.Amount);
                
                groupView.OnButtonPressed += OnButtonPressedHandler;
                
                _viewsDict.Add(groupsConfig.GroupType, groupView);
                
                var newCardsAmount = newCardsData.GetNewGroupAmount(groupType);
                UpdateGroupNewCards(groupType, newCardsAmount);
            }
        }

        public void SetGroupsProgress(IReadOnlyList<CollectionProgressSnapshot.GroupProgressSnapshot> collectionData)
        {
            foreach (var groupProgressSnapshot in collectionData)
            {
                if (_viewsDict.TryGetValue(groupProgressSnapshot.GroupType, out var groupView))
                {
                    groupView.SetCollectedAmountProgressStart(groupProgressSnapshot.CollectedAmount, groupProgressSnapshot.TotalAmount);
                }
            }
        }

        public void UpdateGroupsProgressAnimated(EventCardsSaveData collectionData)
        {
            foreach (var groupView in _viewsDict.Values)
            {
                var groupType = groupView.GroupType;
                var totalGroupAmount = collectionData.GetGroupAmount(groupType);
                var collectedGroupAmount = collectionData.GetCollectedGroupAmount(groupType);
                groupView.UpdateCollectedAmount(collectedGroupAmount, totalGroupAmount);
            }
        }

        public void UpdateViews(CardCollectionNewCardsDto newCardsData)
        {
            foreach (var groupView in _viewsDict.Values)
            {
                var groupType = groupView.GroupType;
                var newCardsAmount = newCardsData.GetNewGroupAmount(groupType);
                UpdateGroupNewCards(groupType, newCardsAmount);
            }
        }

        public void SetTimerText(string timerText)
        {
            _timerText.text = timerText;
        }
        
        public void UpdateGroupNewCards(string groupType, int groupAmount)
        {
            if (_viewsDict.TryGetValue(groupType, out var view))
            {
                view.UpdateNewCards(groupAmount);
            }
        }
        
        public async UniTask CreateGroupViews(List<CardGroupsConfig> groupsData)
        {
            await UIUtils.LoadAndSetSpritesAsync(
                groupsData,
                config => config.GroupIcon,
                config => _viewsDict.TryGetValue(config.GroupType, out var view) ? view : null,
                (view, sprite) => view.SetSprite(sprite),
                config => config.GroupIcon);
        }
        
        public void UpdateCollectedAmount(int collectedAmount, int totalAmount)
        {
            _collectedAmountProgressView.UpdateCollectedAmount(collectedAmount, totalAmount);
        }

        public void SetCollectedAmountProgressStart(int collectedAmount, int totalAmount)
        {
            _collectedAmountProgressView.SetPreviousProgress(collectedAmount, totalAmount);
        }
        
        private void OnButtonPressedHandler(string groupType)
        {
            OnGroupButtonPressed?.Invoke(groupType);
        }
        
        public void ShowLoader(bool show)
        {
            _loadingAnimationObject.gameObject.SetActive(show);
        }
        
        public void UpdatePointsAmount(int pointsAmount)
        {
            _cardsCollectionPointsView.SetPointsAmount(pointsAmount);
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _cardsCollectionPointsView.OnViewClicked -= OnPointsViewClickedHandler;
            _collectionRewardButton.onClick.RemoveAllListeners();
            _infoButton.onClick.RemoveAllListeners();
            
            foreach (var view in _viewsDict.Values)
            {
                view.OnButtonPressed -= OnButtonPressedHandler;
            }
        }
    }
}