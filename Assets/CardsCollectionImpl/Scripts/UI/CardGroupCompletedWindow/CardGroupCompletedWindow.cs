using CardCollection.Core;
using CoreResources;
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
        //TODO remove it from here. Add interface and make this logic outside module
        private AnimateCurrency _animateCurrency;
        //TODO remove it from here. Add interface and make this logic outside module
        private ResourceManager _resourceManager;
        private ICardCollectionCacheService _cardCollectionCardCollectionCacheService;
        
        private CardGroupCollectionArgs Args => (CardGroupCollectionArgs) Arguments;
        
        [Inject]
        public void Install(AnimateCurrency animateCurrency, ResourceManager resourceManager, ICardCollectionCacheService cardCollectionCardCollectionCacheService)
        {
            _animateCurrency = animateCurrency;
            _resourceManager = resourceManager;
            _cardCollectionCardCollectionCacheService = cardCollectionCardCollectionCacheService;
        }
        
        protected override void OnShowStart()
        {
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
            
            var rewardViewData = UIUtils.CreateRewardViewData(Args.CollectionRewardsConfigSo, Args.GroupType);
            var animationArgs = new ArgAnimateCurrency(View.AnimationStartPosition, ResourceType.Gold,  rewardViewData.Amount);
            _animateCurrency.Animate(animationArgs, OnAnimationCompleted);
            void OnAnimationCompleted()
            {
                _resourceManager.NotifyAmountChanged(ResourceType.Gold);
            }
        }

        private void CloseWindow()
        {
            UIManager.Hide<CardGroupCompletedWindow>();
        }
    }
}