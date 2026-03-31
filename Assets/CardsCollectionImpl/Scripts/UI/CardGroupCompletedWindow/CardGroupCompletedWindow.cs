using CardCollection.Core;
using UIShared;
using UISystem;
using VContainer;

namespace CardCollectionImpl
{
    public class CardGroupCollectionArgs : WindowArgs
    {
        public readonly string GroupType;
        public readonly string GroupName;
        public readonly EventCardsSaveData EventCardsSaveData;
        public readonly CardCollectionRewardsConfigSO CollectionRewardsConfigSo;
        
        public CardGroupCollectionArgs(EventCardsSaveData eventCardsSaveData, string groupType,  string groupName,
            CardCollectionRewardsConfigSO collectionRewardsConfigSo)
        {
            GroupType = groupType;
            GroupName = groupName;
            EventCardsSaveData = eventCardsSaveData;
            CollectionRewardsConfigSo = collectionRewardsConfigSo;
        }
    }
    
    [Window("CardGroupCompletedWindow")]
    public class CardGroupCompletedWindow : WindowController<CardGroupCompletedView>
    {
        private IAnimationService _animationService;
        private ICardCollectionCacheService _cardCollectionCardCollectionCacheService;
        
        private CardGroupCollectionArgs Args => (CardGroupCollectionArgs) Arguments;
        
        private RewardViewData _rewardViewData;
        
        [Inject]
        public void Install(IAnimationService animationService, ICardCollectionCacheService cardCollectionCardCollectionCacheService)
        {
            _animationService = animationService;
            _cardCollectionCardCollectionCacheService = cardCollectionCardCollectionCacheService;
        }
        
        protected override void OnShowStart()
        {
            _rewardViewData = UIUtils.CreateRewardViewData(Args.CollectionRewardsConfigSo, Args.GroupType);
            View.SetRewardData(_rewardViewData.Icon, _rewardViewData.Amount);
        }
        
        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            
            var totalGroupAmount = _cardCollectionCardCollectionCacheService.GetGroupAmount(Args.EventCardsSaveData, Args.GroupType);
            var collectedGroupAmount = _cardCollectionCardCollectionCacheService.GetCollectedGroupAmount(Args.EventCardsSaveData, Args.GroupType);;

            View.CreateViews(Args.GroupType, Args.GroupName, collectedGroupAmount, totalGroupAmount);
        }
        
        protected override void OnHideStart(bool isClosed)
        {
            View.CloseClick -= CloseWindow;
        }

        protected override void OnHideComplete(bool isClosed)
        {
            View.ResetView();
            
            _animationService.Animate(View.AnimationStartPosition, _rewardViewData.Amount, _rewardViewData.Id);
        }

        private void CloseWindow()
        {
            UIManager.Hide<CardGroupCompletedWindow>();
        }
    }
}