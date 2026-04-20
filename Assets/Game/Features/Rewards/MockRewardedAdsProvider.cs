using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rewards
{
    public sealed class MockRewardedAdsProvider : IRewardedAdsProvider
    {
        private readonly RewardedAdsConfig _config;
        private bool _isInitialized;

        public MockRewardedAdsProvider(RewardedAdsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsInitialized => _isInitialized;

        public bool IsAdReady(string adUnitId)
        {
            return _isInitialized;
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            _isInitialized = true;
            Debug.Log("[RewardAdsMock] Initialize success.");
            return UniTask.CompletedTask;
        }

        public UniTask PreloadAsync(string adUnitId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Mock provider is not initialized.");
            }

            Debug.Log($"[RewardAdsMock] Preload success. AdUnitId={adUnitId}");
            return UniTask.CompletedTask;
        }

        public async UniTask<RewardedShowResult> ShowAsync(string adUnitId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Mock provider is not initialized.");
            }

            var delaySeconds = _config.GetMockDelaySeconds();
            Debug.Log($"[RewardAdsMock] Show started. AdUnitId={adUnitId}, DelaySeconds={delaySeconds:0.00}, Outcome={_config.MockOutcome}");
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);

            return _config.MockOutcome switch
            {
                MockAdsOutcome.Success => RewardedShowResult.Completed,
                MockAdsOutcome.Cancel => RewardedShowResult.Canceled,
                MockAdsOutcome.Fail => RewardedShowResult.Failed,
                _ => RewardedShowResult.Failed
            };
        }
    }
}
