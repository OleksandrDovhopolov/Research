using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public readonly ICardCollectionModule CardCollectionModule;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly CardCollectionRewardsConfigSO RewardsConfigSo;
        public readonly Action<string> OnGroupChanged;
        
        public CardGroupArgs(
            UIManager uiManager, 
            ICardCollectionModule cardCollectionModule, 
            EventCardsSaveData eventCardsSaveData,
            string groupType,
            CardCollectionRewardsConfigSO rewardsConfigSo,
            Action<string> onGroupChanged)
        {
            GroupType = groupType;
            UiManager = uiManager;
            CardCollectionModule = cardCollectionModule;
            EventCardsSaveData = eventCardsSaveData;
            RewardsConfigSo = rewardsConfigSo;
            OnGroupChanged = onGroupChanged;
        }
    }
    
    [Window("CardGroupWindow", WindowType.Popup)]
    public class CardGroupController :  WindowController<CardGroupView>
    {
        private CardGroupArgs Args => (CardGroupArgs) Arguments;

        private List<CardProgressData> GroupCardsData => Args.EventCardsSaveData.GetCardsByGroupType(_currentGroupType);
        
        private List<CardGroupsConfig> _allGroups;
        private int _currentGroupIndex;
        private string _currentGroupType;
        private CancellationTokenSource _resetNewFlagsCts;
        
        protected override void OnShowStart()
        {
            _resetNewFlagsCts = new CancellationTokenSource();
            _allGroups = CardGroupsConfigStorage.Instance.Data;
            _currentGroupType = Args.GroupType;
            _currentGroupIndex = _allGroups.FindIndex(g => g.GroupType == _currentGroupType);
            
            ShowCurrentGroup();
        }
        
        private void ShowCurrentGroup()
        {
            var ct = _resetNewFlagsCts?.Token ?? CancellationToken.None;
            SetRewardData();
            
            View.CreateDataViews(_currentGroupType, GroupCardsData);
            
            UpdateGroupViewData();
            UpdateCardSprites();
            ResetNewFlagsAsync(GroupCardsData, ct).Forget();
        }

        private void SetRewardData()
        {
            var rewardViewData = UIUtils.CreateRewardViewData(Args.RewardsConfigSo, _currentGroupType);
            View.SetRewardData(rewardViewData.Icon, rewardViewData.Amount);
        }
        
        private async UniTask ResetNewFlagsAsync(List<CardProgressData> groupData, CancellationToken ct)
        {
            var newCardIds = groupData
                .Where(cardData => cardData.IsNew && !string.IsNullOrEmpty(cardData.CardId))
                .Select(cardData => cardData.CardId)
                .Distinct()
                .ToArray();

            if (newCardIds.Length == 0)
            {
                return;
            }

            await Args.CardCollectionModule.ResetNewFlagsAsync(newCardIds, ct);
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

            _resetNewFlagsCts?.Cancel();
            _resetNewFlagsCts?.Dispose();
            _resetNewFlagsCts = null;
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

            var ct = _resetNewFlagsCts?.Token ?? CancellationToken.None;
            
            _currentGroupIndex = (_currentGroupIndex + direction + _allGroups.Count) % _allGroups.Count;
            _currentGroupType = _allGroups[_currentGroupIndex].GroupType;
            
            var groupCards = Args.EventCardsSaveData.GetCardsByGroupType(_currentGroupType);
            
            await View.AnimateSwitchGroup(direction, _currentGroupType, groupCards, UpdateGroupViewData);

            UpdateCardSprites();
            Args.OnGroupChanged?.Invoke(_currentGroupType);
            await ResetNewFlagsAsync(groupCards, ct);
        }

        private void UpdateCardSprites()
        {
            var configs = CardCollectionConfigStorage.Instance.Get(_currentGroupType);
            SetCardSprites(configs).Forget();
        }
        
        private void UpdateGroupViewData()
        {
            SetRewardData();
            
            var collectionNumberText = "Set " + (_currentGroupIndex + 1) + "/" + _allGroups.Count;
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