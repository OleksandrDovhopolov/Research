using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace Rewards
{
    public sealed class RewardedAdsRewardOrchestrator
    {
        private readonly UIManager _uiManager;
        private readonly AdsRewardFlowService _adsRewardFlowService;
        private readonly RewardedAdsConfig _config;

        public RewardedAdsRewardOrchestrator(
            UIManager uiManager,
            AdsRewardFlowService adsRewardFlowService,
            RewardedAdsConfigSO configSo)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _adsRewardFlowService = adsRewardFlowService ?? throw new ArgumentNullException(nameof(adsRewardFlowService));
            _config = (configSo ?? throw new ArgumentNullException(nameof(configSo))).GetOrCreate();
        }

        public event Action<RewardAdFlowState> StateChanged
        {
            add => _adsRewardFlowService.StateChanged += value;
            remove => _adsRewardFlowService.StateChanged -= value;
        }

        public RewardAdFlowState State => _adsRewardFlowService.State;

        public bool IsReady => _adsRewardFlowService.IsReady;

        public bool IsFlowInProgress => _adsRewardFlowService.IsFlowInProgress;

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            return _adsRewardFlowService.InitializeAsync(ct);
        }

        public async UniTask<RewardGrantFlowResult> TryRunFlowAsync(CancellationToken ct = default)
        {
            var result = await _adsRewardFlowService.TryRunFlowAsync(ct);
            if (result?.Type == RewardGrantFlowResultType.Success)
            {
                if (string.IsNullOrWhiteSpace(_config.RewardId))
                {
                   throw new Exception($"[{nameof(RewardedAdsRewardOrchestrator)}] Reward id is empty.");
                }
                
                var rewardArgs = new RewardsWindowArgs(_config.RewardId);
                _uiManager.Show<RewardsWindowController>(rewardArgs);
            }

            return result;
        }
    }
}
