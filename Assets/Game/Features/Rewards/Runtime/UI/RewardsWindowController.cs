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

        private RewardSpecResource _rewardSpecResource;
        
        [Inject]
        private void Construct(IAnimationService animationService, IRewardSpecProvider rewardSpecProvider)
        {
            _animationService = animationService;
            _rewardSpecProvider = rewardSpecProvider;
        }

        protected override void OnShowStart()
        {
            _rewardSpecResource = GetPrimaryRewardResource(Args.RewardId);
            View.SetReward(_rewardSpecResource.Icon, _rewardSpecResource.Amount);
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
            if (_animationService != null && Args != null && _rewardSpecResource.Amount > 0)
            {
                View.TryGetAnimationStartPosition(out var animationStartPosition);
                _animationService.Animate(animationStartPosition, _rewardSpecResource.Amount, _rewardSpecResource.ResourceId, _rewardSpecResource.Icon);
            }

            View.ResetView();
        }

        private void CloseWindow()
        {
            UIManager.Hide<RewardsWindowController>();
        }
        
        //TODO now returns only first reward
        private RewardSpecResource GetPrimaryRewardResource(string rewardId)
        {
            if (!_rewardSpecProvider.TryGet(rewardId, out var rewardSpec))
            {
                throw new InvalidOperationException($"Unknown reward id: {rewardId}");
            }
            
            var resources = rewardSpec?.Resources;
            if (resources == null || resources.Count == 0)
            {
                throw new InvalidOperationException($"Reward spec '{rewardSpec?.RewardId}' has no resources.");
            }

            foreach (var resource in resources)
            {
                if (resource == null || string.IsNullOrWhiteSpace(resource.ResourceId) || resource.Amount <= 0)
                {
                    continue;
                }

                return resource;
            }

            throw new InvalidOperationException($"Reward spec '{rewardSpec?.RewardId}' has no primary resource.");
        }
    }
}
