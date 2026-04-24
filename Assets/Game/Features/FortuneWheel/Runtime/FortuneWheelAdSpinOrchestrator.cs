using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Rewards;

namespace FortuneWheel
{
    public sealed class FortuneWheelAdSpinOrchestrator
    {
        private readonly AdsRewardFlowService _adsRewardFlowService;

        public FortuneWheelAdSpinOrchestrator(AdsRewardFlowService adsRewardFlowService)
        {
            _adsRewardFlowService = adsRewardFlowService ?? throw new ArgumentNullException(nameof(adsRewardFlowService));
        }

        public UniTask<RewardGrantFlowResult> TryRunFlowAsync(CancellationToken ct = default)
        {
            return _adsRewardFlowService.TryRunFlowForRewardAsync(FortuneWheelConfig.Gameplay.AdSpinRewardId, ct);
        }
    }
}
