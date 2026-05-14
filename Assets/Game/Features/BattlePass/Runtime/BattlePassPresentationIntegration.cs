using System;
using System.Collections.Generic;
using System.Linq;
using Rewards;
using UIShared;
using UISystem;

namespace BattlePass
{
    public sealed class UiManagerBattlePassWindowRouter : IBattlePassWindowRouter
    {
        private readonly UIManager _uiManager;
        private readonly IRewardSpecProvider _rewardSpecProvider;

        public UiManagerBattlePassWindowRouter(UIManager uiManager, IRewardSpecProvider rewardSpecProvider)
        {
            _uiManager = uiManager;
            _rewardSpecProvider = rewardSpecProvider;
        }

        public void ShowInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || _uiManager == null)
            {
                return;
            }

            _uiManager.Show<InfoWidgetController>(new InfoWidgetArg(message));
        }

        public void ShowBattlePassWindow()
        {
            _uiManager?.Show<BattlePassWindowController>();
        }

        public void ShowPremiumPurchase(BattlePassIAPWindowArgs args)
        {
            if (_uiManager == null || args == null)
            {
                return;
            }

            _uiManager.Show<BattlePassIAPWindowController>(args);
        }

        public void ShowGrantedRewards(IReadOnlyList<BattlePassGrantedRewardCell> grantedRewards)
        {
            if (_uiManager == null)
            {
                return;
            }

            var rewardId = grantedRewards?
                .FirstOrDefault(reward => reward != null && !string.IsNullOrWhiteSpace(reward.RewardId))
                ?.RewardId;
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return;
            }

            if (_rewardSpecProvider != null && !_rewardSpecProvider.TryGet(rewardId, out _))
            {
                return;
            }

            _uiManager.Show<RewardsWindowController>(new RewardsWindowArgs(rewardId));
        }

        public void HideBattlePassWindow()
        {
            _uiManager?.Hide<BattlePassWindowController>();
        }

        public void HideBattlePassPurchaseWindow()
        {
            _uiManager?.Hide<BattlePassIAPWindowController>();
        }
    }

    public sealed class UiManagerBattlePassClaimFlowLock : IBattlePassClaimFlowLock
    {
        private static readonly object ClaimFlowLockType = new();

        private readonly UIManager _uiManager;

        public UiManagerBattlePassClaimFlowLock(UIManager uiManager)
        {
            _uiManager = uiManager;
        }

        public IDisposable Acquire()
        {
            if (_uiManager == null || _uiManager.UiFilter == null)
            {
                return NoOpDisposable.Instance;
            }

            return _uiManager.SetManualLock(ClaimFlowLockType);
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }

    public sealed class RewardPlayerStateRefreshBattlePassPostClaimSync : IBattlePassPostClaimSync
    {
        private readonly IRewardPlayerStateRefreshCoordinator _refreshCoordinator;

        public RewardPlayerStateRefreshBattlePassPostClaimSync(IRewardPlayerStateRefreshCoordinator refreshCoordinator)
        {
            _refreshCoordinator = refreshCoordinator;
        }

        public void RequestBackgroundRefresh()
        {
            _refreshCoordinator?.RequestBackgroundRefresh();
        }
    }
}
