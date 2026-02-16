using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardGroupArgs : WindowArgs
    {
        public readonly string GroupType;
        public readonly UIManager UiManager;
        public readonly ICardCollectionModule CardCollectionModule;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly List<CardProgressData> GroupData;
        public readonly int CollectedAmount;
        public readonly int TotalAmount;
        
        public CardGroupArgs(
            UIManager uiManager, 
            ICardCollectionModule cardCollectionModule, 
            EventCardsSaveData eventCardsSaveData,
            string groupType, 
            List<CardProgressData> groupData,
            int collectedAmount,
            int totalAmount)
        {
            GroupType = groupType;
            UiManager = uiManager;
            CardCollectionModule = cardCollectionModule;
            EventCardsSaveData = eventCardsSaveData;
            GroupData = groupData;
            CollectedAmount = collectedAmount;
            TotalAmount = totalAmount;
        }
    }
    
    [Window("CardGroupWindow", WindowType.Popup)]
    public class CardGroupController :  WindowController<CardGroupView>
    {
        private CardGroupArgs Args => (CardGroupArgs) Arguments;
        
        private List<CardGroupsConfig> _allGroups;
        private int _currentGroupIndex;
        private string _currentGroupType;
        
        protected override void OnShowStart()
        {
            _allGroups = CardGroupsConfigStorage.Instance.Data;
            _currentGroupType = Args.GroupType;
            _currentGroupIndex = _allGroups.FindIndex(g => g.GroupType == _currentGroupType);
            
            ShowCurrentGroup(Args.GroupData);
        }
        
        private void ShowCurrentGroup(List<CardProgressData> groupData)
        {
            View.CreateDataViews(_currentGroupType, groupData);
            
            UpdateGroupViewData();
            UpdateCardSprites();
            ResetNewFlag(groupData);
        }

        private void ResetNewFlag(List<CardProgressData> groupData)
        {
            foreach (var cardData in groupData.Where(cardData => cardData.IsNew))
            {
                Args.CardCollectionModule.ResetNewFlagAsync(cardData.CardId);
            }
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
            
            _currentGroupIndex = (_currentGroupIndex + direction + _allGroups.Count) % _allGroups.Count;
            _currentGroupType = _allGroups[_currentGroupIndex].GroupType;
            
            var groupCards = Args.EventCardsSaveData.GetCardsByGroupType(_currentGroupType);
            
            // Animate slide + rebuild cards; update texts while container is off-screen
            await View.AnimateSwitchGroup(direction, _currentGroupType, groupCards, UpdateGroupViewData);

            UpdateCardSprites();
            ResetNewFlag(groupCards);
        }

        private void UpdateCardSprites()
        {
            var configs = CardCollectionConfigStorage.Instance.Get(_currentGroupType);
            SetCardSprites(configs).Forget();
        }
        
        private void UpdateGroupViewData()
        {
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