using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace Rewards
{
    public sealed class MockRewardedAdsProvider : IRewardedAdsProvider
    {
        private const string CallbackPath = "rewards/intent/callback";

        private readonly RewardedAdsConfig _config;
        private readonly IWebClient _webClient;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private bool _isInitialized;

        public MockRewardedAdsProvider(
            RewardedAdsConfig config,
            IWebClient webClient = null,
            IPlayerIdentityProvider playerIdentityProvider = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _webClient = webClient;
            _playerIdentityProvider = playerIdentityProvider;
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

        public async UniTask<RewardedShowResult> ShowAsync(string adUnitId, string rewardIntentId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Mock provider is not initialized.");
            }

            var delaySeconds = _config.GetMockDelaySeconds();
            Debug.Log($"[RewardAdsMock] Show started. AdUnitId={adUnitId}, RewardIntentId={rewardIntentId}, DelaySeconds={delaySeconds:0.00}, Outcome={_config.MockOutcome}");
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);

            var showResult = _config.MockOutcome switch
            {
                MockAdsOutcome.Success => RewardedShowResult.Completed,
                MockAdsOutcome.Cancel => RewardedShowResult.Canceled,
                MockAdsOutcome.Fail => RewardedShowResult.Failed,
                _ => RewardedShowResult.Failed
            };

            if (showResult == RewardedShowResult.Completed && !string.IsNullOrWhiteSpace(rewardIntentId))
            {
                await EmulateServerCallbackAsync(rewardIntentId, ct);
            }

            return showResult;
        }

        private async UniTask EmulateServerCallbackAsync(string rewardIntentId, CancellationToken ct)
        {
            if (_webClient == null)
            {
                Debug.LogWarning("[RewardAdsMock] Callback emulation skipped: IWebClient is not available.");
                return;
            }

            var playerId = _playerIdentityProvider?.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                Debug.LogWarning(
                    "[RewardAdsMock] PlayerId is empty. Using rewardIntentId as fallback rewardIntentId query value.");
                playerId = rewardIntentId;
            }

            var eventId = $"mock_{Guid.NewGuid():N}";
            var callbackUrl =
                $"{CallbackPath}?rewardIntentId={Uri.EscapeDataString(playerId)}&eventId={Uri.EscapeDataString(eventId)}&rewards=1&dynamicUserId={Uri.EscapeDataString(rewardIntentId)}";

            try
            {
                Debug.Log(
                    $"[RewardAdsMock] Callback emulation (GET) started. PlayerId={playerId}, DynamicUserId={rewardIntentId}, EventId={eventId}");
                await _webClient.GetAsync<object>(callbackUrl, ct);
                Debug.Log(
                    $"[RewardAdsMock] Callback emulation success. PlayerId={playerId}, DynamicUserId={rewardIntentId}, EventId={eventId}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[RewardAdsMock] Callback emulation failed. PlayerId={playerId}, DynamicUserId={rewardIntentId}, EventId={eventId}, Reason={exception.Message}");
            }
        }
    }
}
