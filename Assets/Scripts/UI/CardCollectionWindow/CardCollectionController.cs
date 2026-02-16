using System;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardCollectionArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly ICardCollectionModule CardCollectionModule;
        public readonly EventCardsSaveData EventCardsSaveData;
        
        public CardCollectionArgs(UIManager uiManager, ICardCollectionModule cardCollectionModule, EventCardsSaveData  eventCardsSaveData)
        {
            UiManager = uiManager;
            CardCollectionModule = cardCollectionModule;
            EventCardsSaveData = eventCardsSaveData;
        }
    }
    
    [Window("CardCollectionWindow")]
    public class CardCollectionController :  WindowController<CardCollectionView>
    {
        private CardCollectionArgs Args => (CardCollectionArgs) Arguments;
        
        private bool _groupsCreated;
        
        protected override void OnShowStart()
        {
            if (_groupsCreated)
            {
                View.UpdateViews(Args.EventCardsSaveData);
            }
            else
            {
                View.ShowLoader(true); 
                View.CreateViews(Args.EventCardsSaveData);
            }
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnGroupButtonPressed += OnGroupButtonPressedHandler;
            
            if (_groupsCreated) return;
            CreateGroupViews().Forget();
        }

        private async UniTask CreateGroupViews()
        {
            try
            {
                await View.CreateGroupViews(CardGroupsConfigStorage.Instance.Data);
                _groupsCreated = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load groups: {e}");
            }
            finally
            {
                View.ShowLoader(false);
            }
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnGroupButtonPressed -= OnGroupButtonPressedHandler;
        }

        private void OnGroupButtonPressedHandler(string groupType)
        {
            View.UpdateGroupNewCards(groupType, 0);
            
            var groupCards = Args.EventCardsSaveData.GetCardsByGroupType(groupType);
            var groupsAmount = EventCardsSaveDataExtensions.GetGroupCount();
            var groupNumber = EventCardsSaveDataExtensions.GetGroupTypeIndex(groupType);
            var args = new CardGroupArgs(Args.UiManager, Args.CardCollectionModule, groupType, groupCards, groupNumber, groupsAmount);
            Args.UiManager.Show<CardGroupController>(args);
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<CardCollectionController>();
        }
    }
}