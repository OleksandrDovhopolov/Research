using System.Collections.Generic;
using UIShared;
using UISystem;
using UnityEngine;

namespace Rewards
{
    public class RewardsWindowView : WindowView
    {
        [SerializeField] private UIListPool<RewardItemView> _cardGroupsPool;

        private readonly Dictionary<RewardSpecResource, RewardItemView> _rewardItemViews = new();
        
        public void SetReward(List<RewardSpecResource> rewardSpecResources)
        {
            _rewardItemViews.Clear();
            _cardGroupsPool.DisableNonActive();
            
            foreach (var rewardSpecResource in rewardSpecResources)
            {
                var rewardItemView = _cardGroupsPool.GetNext();
                rewardItemView.SetResourceData(rewardSpecResource);
                _rewardItemViews.Add(rewardSpecResource, rewardItemView);
            }
        }
        
        public Dictionary<RewardSpecResource, RewardItemView> GetViews()
        {
            return _rewardItemViews;
        }

        public void ResetView()
        {
            foreach (var rewardItemView in _rewardItemViews.Values)
            {
                rewardItemView.ResetView();
            }
            _rewardItemViews.Clear();
            _cardGroupsPool.DisableAll();
        }
    }
}
