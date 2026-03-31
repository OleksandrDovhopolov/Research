using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using UIShared;
using UISystem;
using VContainer;

namespace CardCollectionImpl
{
    public class CardGroupCollectionArgs : WindowArgs
    {
        public readonly string EventId;
        public readonly List<CardCollectionGroupConfig> GroupConfigs;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly CardCollectionRewardsConfigSO CollectionRewardsConfigSo;
        
        public CardGroupCollectionArgs(
            string eventId,
            EventCardsSaveData eventCardsSaveData, 
            List<CardCollectionGroupConfig>  groupConfigs,
            CardCollectionRewardsConfigSO collectionRewardsConfigSo)
        {
            EventId = eventId;
            GroupConfigs = groupConfigs;
            EventCardsSaveData = eventCardsSaveData;
            CollectionRewardsConfigSo = collectionRewardsConfigSo;
        }
    }
    
    [Window("CardGroupCompletedWindow")]
    public class CardGroupCompletedWindow : WindowController<CardGroupCompletedView>
    {
        private IAnimationService _animationService;
        private IEventSpriteManager _eventSpriteManager;
        private ICardCollectionCacheService _cardCollectionCardCollectionCacheService;
        
        private CardGroupCollectionArgs Args => (CardGroupCollectionArgs) Arguments;
        
        private readonly Dictionary<string, RewardViewData> _groupRewardsByType = new();
        
        [Inject]
        public void Install(IAnimationService animationService, IEventSpriteManager eventSpriteManager, ICardCollectionCacheService cardCollectionCardCollectionCacheService)
        {
            _animationService = animationService;
            _eventSpriteManager = eventSpriteManager;
            _cardCollectionCardCollectionCacheService = cardCollectionCardCollectionCacheService;
        }
        
        protected override void OnShowStart()
        {
            var groupsDataByType = new List<CardGroupCompletedView.GroupCompletedViewData>();
            _groupRewardsByType.Clear();
            
            foreach (var groupConfig in Args.GroupConfigs ?? Enumerable.Empty<CardCollectionGroupConfig>())
            {
                var groupType = groupConfig.groupType;
                if (string.IsNullOrWhiteSpace(groupType))
                {
                    continue;
                }

                var totalGroupAmount = _cardCollectionCardCollectionCacheService.GetGroupAmount(Args.EventCardsSaveData, groupType);
                var collectedGroupAmount = _cardCollectionCardCollectionCacheService.GetCollectedGroupAmount(Args.EventCardsSaveData, groupType);
                var rewardViewData = UIUtils.CreateRewardViewData(Args.CollectionRewardsConfigSo, groupType);

                _groupRewardsByType[groupType] = rewardViewData;

                var viewData = new CardGroupCompletedView.GroupCompletedViewData(
                    groupType,
                    groupConfig.groupName,
                    groupConfig.groupIcon,
                    collectedGroupAmount,
                    totalGroupAmount,
                    rewardViewData.Icon,
                    rewardViewData.Amount);
                groupsDataByType.Add(viewData);
            }

            View.CreateViews(Args.EventId, groupsDataByType, _eventSpriteManager);
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }

        protected override void OnHideComplete(bool isClosed)
        {
            foreach (var rewardByType in _groupRewardsByType)
            {
                var groupType = rewardByType.Key;
                var rewardViewData = rewardByType.Value;
                if (rewardViewData.Amount <= 0 || string.IsNullOrWhiteSpace(rewardViewData.Id))
                {
                    continue;
                }

                View.TryGetAnimationStartPosition(groupType, out var animationStartPosition);
                _animationService.Animate(animationStartPosition, rewardViewData.Amount, rewardViewData.Id);
            }

            _groupRewardsByType.Clear();
            View.ResetView();
        }

        private void CloseWindow()
        {
            UIManager.Hide<CardGroupCompletedWindow>();
        }
    }
}