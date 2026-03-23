using System;
using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly string GroupType;
        public readonly UIManager UiManager;
        public readonly CardCollectionNewCardsDto NewCardsData;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly CardCollectionRewardsConfigSO RewardsConfigSo;
        public readonly Action<string> OnGroupChanged;
        public readonly List<CardCollectionGroupConfig> GroupConfigs;
        
        public CardGroupArgs(
            UIManager uiManager, 
            CardCollectionNewCardsDto newCardsData, 
            EventCardsSaveData eventCardsSaveData,
            string groupType,
            CardCollectionRewardsConfigSO rewardsConfigSo,
            Action<string> onGroupChanged,
            List<CardCollectionGroupConfig> groupConfigs)
        {
            GroupType = groupType;
            UiManager = uiManager;
            NewCardsData = newCardsData;
            EventCardsSaveData = eventCardsSaveData;
            RewardsConfigSo = rewardsConfigSo;
            OnGroupChanged = onGroupChanged;
            GroupConfigs = groupConfigs;
        }
    }
    
    [Window("CardGroupWindow", WindowType.Popup)]
    public class CardGroupController :  WindowController<CardGroupView>
    {
        private CardGroupArgs Args => (CardGroupArgs) Arguments;

        private List<CardProgressData> GroupCardsData => Args.EventCardsSaveData.GetCardsByGroupType(_currentGroupType);

        private List<CardCollectionGroupConfig> CollectionGroups => Args.GroupConfigs;
        private int _currentGroupIndex;
        private string _currentGroupType;
        
        protected override void OnShowStart()
        {
            _currentGroupType = Args.GroupType;
            _currentGroupIndex = CollectionGroups.FindIndex(g => g.groupType == _currentGroupType);
            
            ShowCurrentGroup();
        }
        
        private void ShowCurrentGroup()
        {
            SetRewardData();
            
            View.CreateDataViews(_currentGroupType, GroupCardsData, Args.NewCardsData);
            
            UpdateGroupViewData();
            UpdateCardSprites();
            MarkCurrentGroupAsSeen();
        }

        private void SetRewardData()
        {
            var rewardViewData = UIUtils.CreateRewardViewData(Args.RewardsConfigSo, _currentGroupType);
            View.SetRewardData(rewardViewData.Icon, rewardViewData.Amount);
        }
        
        private async UniTask SetCardSprites(List<CardCollectionConfig> cardsData)
        {
            await View.SetSprites(cardsData);
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnLeftClick += OnLeftClickHandler;
            View.OnRightClick += OnRightClickHandler;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnLeftClick -= OnLeftClickHandler;
            View.OnRightClick -= OnRightClickHandler;
            View.DisableAll();
        }

        private void OnLeftClickHandler()
        {
            SwitchGroup(-1).Forget();
        }

        private void OnRightClickHandler()
        {
            SwitchGroup(1).Forget();
        }
        
        private async UniTask SwitchGroup(int direction)
        {
            if (View.IsAnimating) return;

            _currentGroupIndex = (_currentGroupIndex + direction + CollectionGroups.Count) % CollectionGroups.Count;
            _currentGroupType = CollectionGroups[_currentGroupIndex].groupType;
            
            var groupCards = Args.EventCardsSaveData.GetCardsByGroupType(_currentGroupType);
            
            await View.AnimateSwitchGroup(direction, _currentGroupType, groupCards, Args.NewCardsData, UpdateGroupViewData);

            UpdateCardSprites();
            MarkCurrentGroupAsSeen();
            Args.OnGroupChanged?.Invoke(_currentGroupType);
        }

        private void MarkCurrentGroupAsSeen()
        {
            Args.NewCardsData.MarkGroupAsSeen(_currentGroupType);
        }

        private void UpdateCardSprites()
        {
            var configs = CardCollectionConfigStorage.Instance.Get(_currentGroupType);
            SetCardSprites(configs).Forget();
        }
        
        private void UpdateGroupViewData()
        {
            SetRewardData();
            
            var collectionNumberText = "Set " + (_currentGroupIndex + 1) + "/" + CollectionGroups.Count;
            View.SetCollectionNumber(collectionNumberText);
            
            var collectedAmount = Args.EventCardsSaveData.GetCollectedGroupAmount(_currentGroupType);
            var totalAmount = Args.EventCardsSaveData.GetGroupAmount(_currentGroupType);
            View.UpdateCollectedAmount(collectedAmount, totalAmount);
        }
        
        protected override void OnHideComplete(bool isClosed) 
        {
            View.DisableAll();
        }

        private void CloseWindow()
        {
            Args.UiManager.Hide<CardGroupController>();
        }
    }
}