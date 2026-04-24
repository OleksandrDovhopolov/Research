using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

namespace Rewards
{
#if UNITY_ADS
    public sealed class UnityAdsRewardedAdsProvider :
        IRewardedAdsProvider,
        IUnityAdsInitializationListener,
        IUnityAdsLoadListener,
        IUnityAdsShowListener
    {
        private readonly RewardedAdsConfig _config;
        private readonly HashSet<string> _readyAdUnits = new(StringComparer.Ordinal);
        private readonly Dictionary<string, UniTaskCompletionSource<bool>> _loadOperationByAdUnit = new(StringComparer.Ordinal);

        private UniTaskCompletionSource<bool> _initializeOperation;
        private UniTaskCompletionSource<RewardedShowResult> _showOperation;

        public UnityAdsRewardedAdsProvider(RewardedAdsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsInitialized { get; private set; }

        public bool IsAdReady(string adUnitId)
        {
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                return false;
            }

            return _readyAdUnits.Contains(adUnitId);
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (IsInitialized)
            {
                return;
            }

            var gameId = _config.GetGameIdForCurrentPlatform();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new InvalidOperationException("Unity Ads Game ID is not configured for the current platform.");
            }

            if (Advertisement.isInitialized)
            {
                IsInitialized = true;
                Debug.Log("[RewardAdsUnity] Initialize skipped, SDK is already initialized.");
                return;
            }

            if (_initializeOperation != null)
            {
                await _initializeOperation.Task.AttachExternalCancellation(ct);
                return;
            }

            Debug.Log($"[RewardAdsUnity] Initialize started. GameId={gameId}, TestMode={_config.TestMode}");
            _initializeOperation = new UniTaskCompletionSource<bool>();
            Advertisement.Initialize(gameId, _config.TestMode, this);

            try
            {
                await _initializeOperation.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                _initializeOperation = null;
            }
        }

        public async UniTask PreloadAsync(string adUnitId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Unity Ads SDK is not initialized.");
            }

            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                throw new InvalidOperationException("Rewarded ad unit id is empty.");
            }

            if (_readyAdUnits.Contains(adUnitId))
            {
                return;
            }

            if (_loadOperationByAdUnit.TryGetValue(adUnitId, out var existingLoadOperation))
            {
                await existingLoadOperation.Task.AttachExternalCancellation(ct);
                return;
            }

            var loadOperation = new UniTaskCompletionSource<bool>();
            _loadOperationByAdUnit[adUnitId] = loadOperation;
            Debug.Log($"[RewardAdsUnity] Load started. AdUnitId={adUnitId}");
            Advertisement.Load(adUnitId, this);

            try
            {
                await loadOperation.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                _loadOperationByAdUnit.Remove(adUnitId);
            }
        }

        public async UniTask<RewardedShowResult> ShowAsync(string adUnitId, string rewardIntentId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Unity Ads SDK is not initialized.");
            }

            if (!IsAdReady(adUnitId))
            {
                Debug.LogWarning($"[RewardAdsUnity] Show skipped, ad is not ready. AdUnitId={adUnitId}");
                return RewardedShowResult.Failed;
            }

            if (_showOperation != null)
            {
                throw new InvalidOperationException("Another show operation is already in progress.");
            }

            Debug.Log($"[RewardAdsUnity] Show started. AdUnitId={adUnitId}, RewardIntentId={rewardIntentId}");
            _showOperation = new UniTaskCompletionSource<RewardedShowResult>();
            Advertisement.Show(adUnitId, this);

            try
            {
                var result = await _showOperation.Task.AttachExternalCancellation(ct);
                _readyAdUnits.Remove(adUnitId);
                return result;
            }
            finally
            {
                _showOperation = null;
            }
        }

        public void OnInitializationComplete()
        {
            IsInitialized = true;
            Debug.Log("[RewardAdsUnity] Initialize success.");
            _initializeOperation?.TrySetResult(true);
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            IsInitialized = false;
            Debug.LogError($"[RewardAdsUnity] Initialize failed. Error={error}, Message={message}");
            _initializeOperation?.TrySetException(new InvalidOperationException($"Unity Ads initialization failed: {error}. {message}"));
        }

        public void OnUnityAdsAdLoaded(string adUnitId)
        {
            if (!string.IsNullOrWhiteSpace(adUnitId))
            {
                _readyAdUnits.Add(adUnitId);
            }

            Debug.Log($"[RewardAdsUnity] Load success. AdUnitId={adUnitId}");
            if (_loadOperationByAdUnit.TryGetValue(adUnitId ?? string.Empty, out var operation))
            {
                operation.TrySetResult(true);
            }
        }

        public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
        {
            _readyAdUnits.Remove(adUnitId ?? string.Empty);
            Debug.LogWarning($"[RewardAdsUnity] Load failed. AdUnitId={adUnitId}, Error={error}, Message={message}");

            if (_loadOperationByAdUnit.TryGetValue(adUnitId ?? string.Empty, out var operation))
            {
                operation.TrySetException(new InvalidOperationException($"Unity Ads load failed: {error}. {message}"));
            }
        }

        public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
        {
            Debug.LogWarning($"[RewardAdsUnity] Show failed. AdUnitId={adUnitId}, Error={error}, Message={message}");
            _showOperation?.TrySetResult(RewardedShowResult.Failed);
        }

        public void OnUnityAdsShowStart(string adUnitId)
        {
            Debug.Log($"[RewardAdsUnity] Show callback started. AdUnitId={adUnitId}");
        }

        public void OnUnityAdsShowClick(string adUnitId)
        {
            Debug.Log($"[RewardAdsUnity] Show clicked. AdUnitId={adUnitId}");
        }

        public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
        {
            var result = showCompletionState == UnityAdsShowCompletionState.COMPLETED
                ? RewardedShowResult.Completed
                : RewardedShowResult.Canceled;

            Debug.Log($"[RewardAdsUnity] Show completed. AdUnitId={adUnitId}, CompletionState={showCompletionState}");
            _showOperation?.TrySetResult(result);
        }
    }
#else
    public sealed class UnityAdsRewardedAdsProvider : IRewardedAdsProvider
    {
        public UnityAdsRewardedAdsProvider(RewardedAdsConfig config)
        {
        }

        public bool IsInitialized => false;

        public bool IsAdReady(string adUnitId)
        {
            return false;
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            throw new InvalidOperationException("UNITY_ADS scripting define is not enabled. Install/enable Unity Ads package.");
        }

        public UniTask PreloadAsync(string adUnitId, CancellationToken ct = default)
        {
            throw new InvalidOperationException("UNITY_ADS scripting define is not enabled. Install/enable Unity Ads package.");
        }

        public UniTask<RewardedShowResult> ShowAsync(string adUnitId, string rewardIntentId, CancellationToken ct = default)
        {
            throw new InvalidOperationException("UNITY_ADS scripting define is not enabled. Install/enable Unity Ads package.");
        }
    }
#endif
}
