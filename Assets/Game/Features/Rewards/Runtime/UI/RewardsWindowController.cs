using UISystem;
using UIShared;
using UnityEngine;
using VContainer;

namespace Rewards
{
    public class RewardsWindowArgs : WindowArgs
    {
        public Sprite RewardSprite { get; }
        public int RewardAmount { get; }
        public string RewardResourceId { get; }

        public RewardsWindowArgs(Sprite rewardSprite, int rewardAmount, string rewardResourceId)
        {
            RewardSprite = rewardSprite;
            RewardAmount = rewardAmount;
            RewardResourceId = rewardResourceId;
        }
    }
    
    [Window("RewardsWindow", WindowType.Popup)]
    public class RewardsWindowController : WindowController<RewardsWindowView>
    {
        private IAnimationService _animationService;

        private RewardsWindowArgs Args => (RewardsWindowArgs) Arguments;

        [Inject]
        private void Construct(IAnimationService animationService)
        {
            _animationService = animationService;
        }

        protected override void OnShowStart()
        {
            View.SetReward(Args.RewardSprite, Args.RewardAmount);
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
            if (_animationService != null && Args != null && Args.RewardAmount > 0)
            {
                View.TryGetAnimationStartPosition(out var animationStartPosition);
                _animationService.Animate(animationStartPosition, Args.RewardAmount, Args.RewardResourceId, Args.RewardSprite);
            }

            View.ResetView();
        }

        private void CloseWindow()
        {
            UIManager.Hide<RewardsWindowController>();
        }
    }
}
