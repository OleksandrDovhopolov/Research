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
        public readonly IExchangeOfferProvider ExchangeOfferProvider;
        
        public CardCollectionArgs(
            UIManager uiManager,
            ICardCollectionModule cardCollectionModule,
            EventCardsSaveData eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider)
        {
            UiManager = uiManager;
            CardCollectionModule = cardCollectionModule;
            EventCardsSaveData = eventCardsSaveData;
            ExchangeOfferProvider = exchangeOfferProvider;
        }
    }
    
    [Window("CardCollectionWindow")]
    public class CardCollectionController :  WindowController<CardCollectionView>
    {
        private CardCollectionArgs Args => (CardCollectionArgs) Arguments;
        
        private bool _groupsCreated;
        
        protected override void OnShowStart()
        {
            UpdatePointsAmount();
            
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
            View.OnPointsViewClicked += OnPointsViewClickedHandler;
            View.OnGroupButtonPressed += OnGroupButtonPressedHandler;
            
            if (_groupsCreated) return;
            CreateGroupViews().Forget();
        }

        private void OnPointsViewClickedHandler()
        {
            var args = new CollectionPointsExchangeArgs(
                Args.UiManager,
                Args.EventCardsSaveData.Points,
                Args.ExchangeOfferProvider, UpdatePointsAmount);
            Args.UiManager.Show<CollectionPointsExchangeController>(args);
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

        public void UpdatePointsAmount()
        {
            View.UpdatePointsAmount(Args.EventCardsSaveData.Points);
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
            View.OnPointsViewClicked -= OnPointsViewClickedHandler;
            View.OnGroupButtonPressed -= OnGroupButtonPressedHandler;
        }

        private void OnGroupButtonPressedHandler(string groupType)
        {
            View.UpdateGroupNewCards(groupType, 0);
            
            var groupCards = Args.EventCardsSaveData.GetCardsByGroupType(groupType);
            var collectedGroupAmount = Args.EventCardsSaveData.GetCollectedGroupAmount(groupType);
            var totalGroupAmount = Args.EventCardsSaveData.GetGroupAmount(groupType);
            
            var args = new CardGroupArgs(Args.UiManager, Args.CardCollectionModule, Args.EventCardsSaveData, groupType, groupCards, collectedGroupAmount, totalGroupAmount);
            Args.UiManager.Show<CardGroupController>(args);
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<CardCollectionController>();
        }
    }
}