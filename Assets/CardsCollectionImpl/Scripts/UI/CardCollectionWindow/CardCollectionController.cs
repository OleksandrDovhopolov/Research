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
        public readonly CardCollectionNewCardsDto NewCardsData;
        public readonly ICardCollectionPointsAccount CardCollectionPointsAccount;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly IExchangeOfferProvider ExchangeOfferProvider;
        public readonly IRewardDefinitionFactory RewardDefinitionFactory;
        public readonly CollectionProgressSnapshot CollectionProgressSnapshot;
        
        public CardCollectionArgs(
            UIManager uiManager,
            CardCollectionNewCardsDto newCardsData,
            EventCardsSaveData eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CollectionProgressSnapshot  collectionProgressSnapshot)
        {
            UiManager = uiManager;
            NewCardsData = newCardsData;
            EventCardsSaveData = eventCardsSaveData;
            ExchangeOfferProvider = exchangeOfferProvider;
            RewardDefinitionFactory = rewardDefinitionFactory;
            CardCollectionPointsAccount = cardCollectionPointsAccount;
            CollectionProgressSnapshot = collectionProgressSnapshot;
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
                View.UpdateViews(Args.NewCardsData);
            }
            else
            {
                View.ShowLoader(true); 
                View.CreateViews(Args.NewCardsData);
            }

            View.SetGroupsProgress(Args.CollectionProgressSnapshot.GroupProgress);
            Debug.LogWarning($"Test ShowStart {Args.CollectionProgressSnapshot.CollectedAmount} / ");
            View.SetCollectedAmountProgressStart(Args.CollectionProgressSnapshot.CollectedAmount, Args.CollectionProgressSnapshot.TotalAmount);
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.OnPointsViewClicked += OnPointsViewClickedHandler;
            View.OnInfoButtonClicked += OnInfoButtonClickedHandler;
            View.OnRewardChestClicked += OnRewardChestClickedHandler;
            View.OnGroupButtonPressed += OnGroupButtonPressedHandler;

            var collectedAmount = Args.EventCardsSaveData.GetCollectedCardsAmount();
            var totalAmount = Args.EventCardsSaveData.Cards.Count;
            Debug.LogWarning($"Test OnShowComplete {collectedAmount} / ");
            View.UpdateCollectedAmount(collectedAmount, totalAmount);
            View.UpdateGroupsProgressAnimated(Args.EventCardsSaveData);
            
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
                Args.RewardDefinitionFactory,
                Args.CardCollectionPointsAccount,
                UpdatePointsAmount);
            Args.UiManager.Show<CollectionPointsExchangeController>(args);
        }
        
        private void OnInfoButtonClickedHandler()
        {
            var args = new InfoSlidesPageArgs(SlidesType.PiggyBank, Args.UiManager);
            Args.UiManager.Show<InfoSlidesPageController>(args);
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
            View.OnInfoButtonClicked -= OnInfoButtonClickedHandler;
            View.OnRewardChestClicked -= OnRewardChestClickedHandler;
            View.OnGroupButtonPressed -= OnGroupButtonPressedHandler;
        }

        private void OnGroupButtonPressedHandler(string groupType)
        {
            TryHideContentWidget();

            OnGroupViewChangedHandler(groupType);
            
            var args = new CardGroupArgs(
                Args.UiManager, 
                Args.NewCardsData,
                Args.EventCardsSaveData, 
                groupType, 
                View.RewardsConfigSo,
                OnGroupViewChangedHandler);
            Args.UiManager.Show<CardGroupController>(args);
        }

        private void OnGroupViewChangedHandler(string currentGroupType)
        {
            View.UpdateGroupNewCards(currentGroupType, 0);
        }
        
        private void CloseWindow()
        {
            Args.UiManager.Hide<CardCollectionController>();
        }
    }
}