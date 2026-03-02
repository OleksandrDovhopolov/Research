using System;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UIShared;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionArgs : WindowArgs
    {
        public readonly UIManager UiManager;
        public readonly ICardCollectionModule CardCollectionModule;
        public readonly ICardCollectionPointsAccount CardCollectionPointsAccount;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly IExchangeOfferProvider ExchangeOfferProvider;
        public readonly IRewardDefinitionFactory RewardDefinitionFactory;
        
        public CardCollectionArgs(
            UIManager uiManager,
            ICardCollectionModule cardCollectionModule,
            EventCardsSaveData eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount)
        {
            UiManager = uiManager;
            CardCollectionModule = cardCollectionModule;
            EventCardsSaveData = eventCardsSaveData;
            ExchangeOfferProvider = exchangeOfferProvider;
            RewardDefinitionFactory = rewardDefinitionFactory;
            CardCollectionPointsAccount = cardCollectionPointsAccount;
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
            
            var collectedAmount = Args.EventCardsSaveData.GetCollectedCardsAmount();
            var totalAmount = Args.EventCardsSaveData.Cards.Count;
            View.UpdateCollectedAmount(collectedAmount, totalAmount);
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnPointsViewClicked += OnPointsViewClickedHandler;
            View.OnRewardChestClicked += OnRewardChestClickedHandler;
            View.OnGroupButtonPressed += OnGroupButtonPressedHandler;
            
            if (_groupsCreated) return;
            CreateGroupViews().Forget();
        }

        private void OnRewardChestClickedHandler(RectTransform rectTransform)
        {
            var cardCollectionRewardContent = Args.RewardDefinitionFactory.CreateFromCollectionReward();
            var contentWidgetData = cardCollectionRewardContent.ToContentWidgetData();
            var args = new ContentWidgetArgs(Args.UiManager, contentWidgetData, rectTransform);
            Args.UiManager.Show<ContentWidgetController>(args);
        }
        
        private void OnPointsViewClickedHandler()
        {
            TryHideContentWidget();
            
            var args = new CollectionPointsExchangeArgs(
                Args.UiManager,
                Args.EventCardsSaveData.Points,
                Args.ExchangeOfferProvider, 
                (IOfferDefinitionFactory)Args.RewardDefinitionFactory,
                Args.CardCollectionPointsAccount,
                UpdatePointsAmount);
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
        
        private void TryHideContentWidget()
        {
            if (Args.UiManager.IsWindowShown<ContentWidgetController>())
            {
                Args.UiManager.Hide<ContentWidgetController>();
            }
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            TryHideContentWidget();
            
            View.CloseClick -= CloseWindow;
            View.OnPointsViewClicked -= OnPointsViewClickedHandler;
            View.OnRewardChestClicked -= OnRewardChestClickedHandler;
            View.OnGroupButtonPressed -= OnGroupButtonPressedHandler;
        }

        private void OnGroupButtonPressedHandler(string groupType)
        {
            TryHideContentWidget();
            
            View.UpdateGroupNewCards(groupType, 0);
            
            var args = new CardGroupArgs(Args.UiManager, Args.CardCollectionModule, Args.EventCardsSaveData, groupType, View.RewardsConfigSo);
            Args.UiManager.Show<CardGroupController>(args);
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<CardCollectionController>();
        }
    }
}