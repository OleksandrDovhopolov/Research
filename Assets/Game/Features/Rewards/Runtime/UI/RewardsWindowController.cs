using System;
using UIShared;
using UISystem;
using VContainer;

namespace Rewards
{
    public class RewardsWindowArgs : WindowArgs
    {
        public string RewardId { get; }
        
        public RewardsWindowArgs(string rewardId)
        {
            RewardId = rewardId;
        }
    }
    
    [Window("RewardsWindow", WindowType.Popup)]
    public class RewardsWindowController : WindowController<RewardsWindowView>
    {
        private IAnimationService _animationService;
        private IRewardSpecProvider _rewardSpecProvider;

        private RewardsWindowArgs Args => (RewardsWindowArgs) Arguments;
        
        [Inject]
        private void Construct(IAnimationService animationService, IRewardSpecProvider rewardSpecProvider)
        {
            _animationService = animationService;
            _rewardSpecProvider = rewardSpecProvider;
        }

        protected override void OnShowStart()
        {
            if (_rewardSpecProvider.TryGet(Args.RewardId, out var rewardSpec))
            {
                View.SetReward(rewardSpec.Resources);
            }
            else
            {
                throw new InvalidOperationException($"Unknown reward id: {Args.RewardId}");
            }
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
            foreach (var (key, value) in View.GetViews())
            {
                value.TryGetAnimationStartPosition(out var animationStartPosition);
                _animationService.Animate(animationStartPosition, key.Amount, key.ResourceId, key.Icon);
            }
            View.ResetView();
        }

        private void CloseWindow()
        {
            UIManager.Hide<RewardsWindowController>();
        }
    }
}
