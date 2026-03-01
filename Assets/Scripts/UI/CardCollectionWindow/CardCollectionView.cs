using System;
using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CardCollectionView : WindowView
    {
        [SerializeField] private UIListPool<CardsCollectionView> _cardGroupsPool;
        [SerializeField] private GameObject _loadingAnimationObject;
        [SerializeField] private CardCollectionRewardsConfigSO _cardCollectionRewardsConfigSo;
        
        [Header("Points Container")]
        [SerializeField] private CardsCollectionPointsView _cardsCollectionPointsView;
        
        [Space, Space, Header("GroupReward")]
        [SerializeField] private Image _collectedSlider;
        [SerializeField] private TextMeshProUGUI _groupCollectedAmountText;
        
        private readonly Dictionary<string, CardsCollectionView> _viewsDict = new();

        public CardCollectionRewardsConfigSO RewardsConfigSo => _cardCollectionRewardsConfigSo;
        
        public event Action<string> OnGroupButtonPressed;
        public event Action OnPointsViewClicked;
        
        private bool _groupsCreated;

        private void Start()
        {
            _cardsCollectionPointsView.OnViewClicked += OnPointsViewClickedHandler;
        }

        private void OnPointsViewClickedHandler()
        {
            OnPointsViewClicked?.Invoke();
        }
        
        public void CreateViews(EventCardsSaveData collectionData)
        {
            _cardGroupsPool.DisableNonActive();

            _viewsDict.Clear();

            var configs = CardGroupsConfigStorage.Instance.Data;
            
            foreach (var groupsConfig in configs)
            {
                var groupView = _cardGroupsPool.GetNext();
                
                var groupType = groupsConfig.GroupType;
                var groupName = groupsConfig.GroupName;
                var totalGroupAmount = collectionData.GetGroupAmount(groupType);
                var collectedGroupAmount = collectionData.GetCollectedGroupAmount(groupType);
                
                groupView.SetData(groupType, groupName, collectedGroupAmount, totalGroupAmount);
                var rewardViewData = UIUtils.CreateRewardViewData(_cardCollectionRewardsConfigSo, groupType);
                groupView.SetRewardData(rewardViewData.Icon, rewardViewData.Amount);
                
                groupView.OnButtonPressed += OnButtonPressedHandler;
                
                _viewsDict.Add(groupsConfig.GroupType, groupView);
                
                var newCardsAmount = collectionData.GetNewGroupAmount(groupType);
                UpdateGroupNewCards(groupType, newCardsAmount);
            }
        }

        public void UpdateViews(EventCardsSaveData collectionData)
        {
            foreach (var groupView in _viewsDict.Values)
            {
                var groupType = groupView.GroupType;
                var totalGroupAmount = collectionData.GetGroupAmount(groupType);
                var collectedGroupAmount = collectionData.GetCollectedGroupAmount(groupType);
                
                groupView.UpdateCollectedAmount(collectedGroupAmount, totalGroupAmount);
                
                var newCardsAmount = collectionData.GetNewGroupAmount(groupType);
                UpdateGroupNewCards(groupType, newCardsAmount);
            }
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
            _collectedSlider.fillAmount = (float)collectedAmount / totalAmount;;
            _groupCollectedAmountText.text = collectedAmount + " / " + totalAmount;
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
            
            foreach (var view in _viewsDict.Values)
            {
                view.OnButtonPressed -= OnButtonPressedHandler;
            }
        }
    }
}