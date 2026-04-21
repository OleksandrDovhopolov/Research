using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rewards
{
#if UNITY_LEVELPLAY
    public sealed class LevelPlayRewardedAdsProvider : IRewardedAdsProvider
    {
        private readonly RewardedAdsConfig _config;
        private readonly HashSet<string> _readyAdUnits = new(StringComparer.Ordinal);

        public LevelPlayRewardedAdsProvider(RewardedAdsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsInitialized { get; private set; }

        public bool IsAdReady(string adUnitId)
        {
            return !string.IsNullOrWhiteSpace(adUnitId) && _readyAdUnits.Contains(adUnitId);
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (IsInitialized)
            {
                return UniTask.CompletedTask;
            }

            var appKey = _config.GetLevelPlayAppKeyForCurrentPlatform();
            if (string.IsNullOrWhiteSpace(appKey))
            {
                throw new InvalidOperationException("LevelPlay app key is not configured for the current platform.");
            }

            // NOTE: SDK wiring point for real LevelPlay initialization callbacks.
            IsInitialized = true;
            Debug.Log("[RewardAdsLevelPlay] Initialize success.");
            return UniTask.CompletedTask;
        }

        public UniTask PreloadAsync(string adUnitId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!IsInitialized)
            {
                throw new InvalidOperationException("LevelPlay SDK is not initialized.");
            }

            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                throw new InvalidOperationException("LevelPlay rewarded ad unit id is empty.");
            }

            // NOTE: SDK wiring point for real LevelPlay load callbacks.
            _readyAdUnits.Add(adUnitId);
            Debug.Log($"[RewardAdsLevelPlay] Load success. AdUnitId={adUnitId}");
            return UniTask.CompletedTask;
        }

        public UniTask<RewardedShowResult> ShowAsync(string adUnitId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!IsInitialized)
            {
                throw new InvalidOperationException("LevelPlay SDK is not initialized.");
            }

            if (!IsAdReady(adUnitId))
            {
                Debug.LogWarning($"[RewardAdsLevelPlay] Show skipped, ad is not ready. AdUnitId={adUnitId}");
                return UniTask.FromResult(RewardedShowResult.Failed);
            }

            // NOTE: SDK wiring point for real LevelPlay show callbacks and completion mapping.
            _readyAdUnits.Remove(adUnitId);
            Debug.LogWarning($"[RewardAdsLevelPlay] Show flow is not wired to SDK callbacks yet. AdUnitId={adUnitId}");
            return UniTask.FromResult(RewardedShowResult.Failed);
        }
    }
#else
    public sealed class LevelPlayRewardedAdsProvider : IRewardedAdsProvider
    {
        public LevelPlayRewardedAdsProvider(RewardedAdsConfig config)
        {
        }

        public bool IsInitialized => false;

        public bool IsAdReady(string adUnitId)
        {
            return false;
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            throw new InvalidOperationException(
                "UNITY_LEVELPLAY scripting define is not enabled. Install/enable the LevelPlay SDK package.");
        }

        public UniTask PreloadAsync(string adUnitId, CancellationToken ct = default)
        {
            throw new InvalidOperationException(
                "UNITY_LEVELPLAY scripting define is not enabled. Install/enable the LevelPlay SDK package.");
        }

        public UniTask<RewardedShowResult> ShowAsync(string adUnitId, CancellationToken ct = default)
        {
            throw new InvalidOperationException(
                "UNITY_LEVELPLAY scripting define is not enabled. Install/enable the LevelPlay SDK package.");
        }
    }
#endif
}
