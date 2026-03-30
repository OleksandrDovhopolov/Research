using System;
using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UIShared;
using UISystem;
using UnityEngine;
using VContainer;

namespace CardCollectionImpl
{
    public class CardCollectionArgs : WindowArgs
    {
        public readonly CardCollectionNewCardsDto NewCardsData;
        public readonly IReadOnlyList<CardConfig> Cards;
        public readonly IReadOnlyList<CardCollectionGroupConfig> Groups;
        public readonly ICardCollectionPointsAccount CardCollectionPointsAccount;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly IExchangeOfferProvider ExchangeOfferProvider;
        public readonly IRewardDefinitionFactory RewardDefinitionFactory;
        public readonly CollectionProgressSnapshot CollectionProgressSnapshot;
        public readonly CardCollectionRewardsConfigSO CollectionRewardsConfigSo;
        public readonly string ScheduleItemEventId;

        public CardCollectionArgs(CardCollectionNewCardsDto newCardsData,
            EventCardsSaveData eventCardsSaveData,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            CardCollectionRewardsConfigSO collectionRewardsConfigSo,
            ICardCollectionPointsAccount cardCollectionPointsAccount,
            CollectionProgressSnapshot collectionProgressSnapshot,
            string scheduleItemEventId,
            IReadOnlyList<CardConfig> cards,
            IReadOnlyList<CardCollectionGroupConfig> groups)
        {
            NewCardsData = newCardsData;
            Cards = cards;
            Groups = groups;
            EventCardsSaveData = eventCardsSaveData;
            ExchangeOfferProvider = exchangeOfferProvider;
            RewardDefinitionFactory = rewardDefinitionFactory;
            CardCollectionPointsAccount = cardCollectionPointsAccount;
            CollectionRewardsConfigSo = collectionRewardsConfigSo;
            CollectionProgressSnapshot = collectionProgressSnapshot;
            ScheduleItemEventId = scheduleItemEventId;
        }
    }
    
    [Window("CardCollectionWindow")]
    public class CardCollectionController : WindowController<CardCollectionView>
    {
        private IGlobalTimerService _globalTimerService;
        private ICardCollectionCacheService _cardCollectionCardCollectionCacheService;
        
        private CardCollectionArgs Args => (CardCollectionArgs) Arguments;
        private IReadOnlyList<CardCollectionGroupConfig> GroupConfigs => Args.Groups;
        
        private bool _groupsCreated;
        private string _lastRenderedEventId;

        [Inject]
        public void Install(IGlobalTimerService globalTimerService, ICardCollectionCacheService cardCollectionCardCollectionCacheService)
        {
            _globalTimerService = globalTimerService;
            _cardCollectionCardCollectionCacheService = cardCollectionCardCollectionCacheService;
        }
        
        protected override void OnShowStart()
        {
            var currentEventId = Args.ScheduleItemEventId;
            if (!string.Equals(_lastRenderedEventId, currentEventId, StringComparison.Ordinal))
            {
                _groupsCreated = false;
                _lastRenderedEventId = currentEventId;
            }

            View.SetService(_cardCollectionCardCollectionCacheService);

            View.BindEventTimerDisplay(_globalTimerService, Args.ScheduleItemEventId);

            UpdatePointsAmount();
            
            if (_groupsCreated)
            {
                View.UpdateViews(Args.NewCardsData);
            }
            else
            {
                View.ShowLoader(true); 
                View.CreateViews(Args.NewCardsData, GroupConfigs, Args.CollectionRewardsConfigSo);
            }

            View.SetGroupsProgress(Args.CollectionProgressSnapshot.GroupProgress);
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
            
            View.UpdateCollectedAmount(collectedAmount, totalAmount);
            View.UpdateGroupsProgressAnimated(Args.EventCardsSaveData, Args.Cards);
            
            if (_groupsCreated) return;
            CreateGroupViews().Forget();
        }

        private void OnRewardChestClickedHandler(RectTransform rectTransform)
        {
            var cardCollectionRewardContent = Args.RewardDefinitionFactory.CreateFromCollectionReward();
            var contentWidgetData = cardCollectionRewardContent.ToContentWidgetData();
            var args = new ContentWidgetArgs(contentWidgetData, rectTransform);
            UIManager.Show<ContentWidgetController>(args);
        }
        
        private void OnPointsViewClickedHandler()
        {
            TryHideContentWidget();
            
            var args = new CollectionPointsExchangeArgs(
                Args.EventCardsSaveData.Points,
                Args.ExchangeOfferProvider, 
                Args.RewardDefinitionFactory,
                Args.CardCollectionPointsAccount,
                UpdatePointsAmount);
            UIManager.Show<CollectionPointsExchangeController>(args);
        }
        
        private void OnInfoButtonClickedHandler()
        {
            var args = new InfoSlidesPageArgs(SlidesType.PiggyBank, UIManager);
            UIManager.Show<InfoSlidesPageController>(args);
        }
        
        private async UniTask CreateGroupViews()
        {
            try
            {
                await View.CreateGroupViews(Args.ScheduleItemEventId, GroupConfigs);
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
            if (UIManager.IsWindowShown<ContentWidgetController>())
            {
                UIManager.Hide<ContentWidgetController>();
            }
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            TryHideContentWidget();

            View.UnbindEventTimerDisplay();

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
                Args.ScheduleItemEventId,
                Args.NewCardsData,
                Args.EventCardsSaveData, 
                groupType,
                Args.Cards,
                Args.Groups,
                Args.CollectionRewardsConfigSo,
                OnGroupViewChangedHandler);
            UIManager.Show<CardGroupController>(args);
        }

        private void OnGroupViewChangedHandler(string currentGroupType)
        {
            View.UpdateGroupNewCards(currentGroupType, 0);
        }
        
        private void CloseWindow()
        {
            UIManager.Hide<CardCollectionController>();
        }
    }
}