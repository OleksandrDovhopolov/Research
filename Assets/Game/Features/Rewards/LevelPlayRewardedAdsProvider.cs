using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
#if UNITY_LEVELPLAY
using System.Collections.Generic;
using Unity.Services.LevelPlay;
#endif

namespace Rewards
{
#if UNITY_LEVELPLAY
    public sealed class LevelPlayRewardedAdsProvider : IRewardedAdsProvider
    {
        private const string RewardedServerParamsMetaDataKey = "LevelPlay_Rewarded_Server_Params";

        private sealed class RewardedAdContext
        {
            public LevelPlayRewardedAd Ad;
            public bool IsReady;
            public bool HasReward;
            public bool HasClosed;
            public UniTaskCompletionSource<bool> LoadOperation;
            public UniTaskCompletionSource<RewardedShowResult> ShowOperation;
        }

        private const float RewardAfterCloseGraceSeconds = 2f;
        private readonly RewardedAdsConfig _config;
        private readonly IPlayerIdentityProvider _identityProvider;
        private readonly Dictionary<string, RewardedAdContext> _adContexts = new(StringComparer.Ordinal);

        private UniTaskCompletionSource<bool> _initializeOperation;
        private bool _initializationCallbacksRegistered;

        public LevelPlayRewardedAdsProvider(RewardedAdsConfig config, IPlayerIdentityProvider playerIdentityProvider)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _identityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
        }

        public bool IsInitialized { get; private set; }

        public bool IsAdReady(string adUnitId)
        {
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                return false;
            }

            if (!_adContexts.TryGetValue(adUnitId, out var context))
            {
                return false;
            }

            return context.IsReady || context.Ad.IsAdReady();
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (IsInitialized)
            {
                return;
            }

            var appKey = _config.GetLevelPlayAppKeyForCurrentPlatform();
            if (string.IsNullOrWhiteSpace(appKey))
            {
                throw new InvalidOperationException("LevelPlay app key is not configured for the current platform.");
            }

            if (_initializeOperation != null)
            {
                await _initializeOperation.Task.AttachExternalCancellation(ct);
                return;
            }

            RegisterInitializationCallbacks();

            Debug.Log("[RewardAdsLevelPlay] Initialize started.");
            _initializeOperation = new UniTaskCompletionSource<bool>();
            LevelPlay.Init(appKey, _identityProvider.GetPlayerId());

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
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                throw new InvalidOperationException("LevelPlay rewarded ad unit id is empty.");
            }

            var context = GetOrCreateContext(adUnitId);
            if (context.IsReady || context.Ad.IsAdReady())
            {
                context.IsReady = true;
                return;
            }

            if (context.LoadOperation != null)
            {
                await context.LoadOperation.Task.AttachExternalCancellation(ct);
                return;
            }

            var loadOperation = new UniTaskCompletionSource<bool>();
            context.LoadOperation = loadOperation;
            Debug.Log($"[RewardAdsLevelPlay] Load started. AdUnitId={adUnitId}");
            context.Ad.LoadAd();

            try
            {
                await loadOperation.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                if (ReferenceEquals(context.LoadOperation, loadOperation))
                {
                    context.LoadOperation = null;
                }
            }
        }

        public async UniTask<RewardedShowResult> ShowAsync(
            string adUnitId,
            string rewardIntentId,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            EnsureInitialized();

            var context = GetOrCreateContext(adUnitId);
            var isReady = context.IsReady || context.Ad.IsAdReady();
            context.IsReady = isReady;
            if (!isReady)
            {
                Debug.LogWarning($"[RewardAdsLevelPlay] Show skipped, ad is not ready. AdUnitId={adUnitId}");
                return RewardedShowResult.Failed;
            }

            if (context.ShowOperation != null)
            {
                throw new InvalidOperationException("Another LevelPlay rewarded show operation is already in progress.");
            }

            context.IsReady = false;
            context.HasReward = false;
            context.HasClosed = false;

            var showOperation = new UniTaskCompletionSource<RewardedShowResult>();
            context.ShowOperation = showOperation;

            ApplyRewardIntentForServerCallback(rewardIntentId);
            Debug.Log($"[RewardAdsLevelPlay] Show started. AdUnitId={adUnitId}, RewardIntentId={rewardIntentId}");
            context.Ad.ShowAd();

            try
            {
                return await showOperation.Task.AttachExternalCancellation(ct);
            }
            finally
            {
                if (ReferenceEquals(context.ShowOperation, showOperation))
                {
                    context.ShowOperation = null;
                }

                context.HasReward = false;
                context.HasClosed = false;
            }
        }

        private static void ApplyRewardIntentForServerCallback(string rewardIntentId)
        {
            if (string.IsNullOrWhiteSpace(rewardIntentId))
            {
                LevelPlay.SetMetaData(RewardedServerParamsMetaDataKey, Array.Empty<string>());
                Debug.LogWarning("[RewardAdsLevelPlay] RewardIntentId is empty. Cleared rewarded S2S custom params.");
                return;
            }

            var isDynamicUserIdSet = LevelPlay.SetDynamicUserId(rewardIntentId);
            
            if (!isDynamicUserIdSet)
            {
                Debug.LogWarning($"[RewardAdsLevelPlay] Failed to set dynamic user id for callback. RewardIntentId={rewardIntentId}");
            }

            //LevelPlay.SetMetaData(RewardedServerParamsMetaDataKey, $"rewardIntentId={rewardIntentId}");
            //Debug.Log($"[RewardAdsLevelPlay] Applied rewarded S2S params. RewardIntentId={rewardIntentId}");
        }

        private void RegisterInitializationCallbacks()
        {
            if (_initializationCallbacksRegistered)
            {
                return;
            }

            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            _initializationCallbacksRegistered = true;
        }

        private RewardedAdContext GetOrCreateContext(string adUnitId)
        {
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                throw new InvalidOperationException("LevelPlay rewarded ad unit id is empty.");
            }

            if (_adContexts.TryGetValue(adUnitId, out var context))
            {
                return context;
            }

            var rewardedAd = new LevelPlayRewardedAd(adUnitId);
            context = new RewardedAdContext
            {
                Ad = rewardedAd
            };

            rewardedAd.OnAdLoaded += adInfo => OnAdLoaded(adUnitId, context, adInfo);
            rewardedAd.OnAdLoadFailed += error => OnAdLoadFailed(adUnitId, context, error);
            rewardedAd.OnAdDisplayed += adInfo => Debug.Log($"[RewardAdsLevelPlay] Show callback started. AdUnitId={adUnitId}");
            rewardedAd.OnAdDisplayFailed += (adInfo, error) => OnAdDisplayFailed(adUnitId, context, adInfo, error);
            rewardedAd.OnAdRewarded += (adInfo, reward) => OnAdRewarded(adUnitId, context, reward);
            rewardedAd.OnAdClicked += adInfo => Debug.Log($"[RewardAdsLevelPlay] Show clicked. AdUnitId={adUnitId}");
            rewardedAd.OnAdClosed += adInfo => OnAdClosed(adUnitId, context);
            rewardedAd.OnAdInfoChanged += adInfo => Debug.Log($"[RewardAdsLevelPlay] Ad info changed. AdUnitId={adUnitId}");

            _adContexts[adUnitId] = context;
            return context;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("LevelPlay SDK is not initialized.");
            }
        }

        private void OnInitSuccess(LevelPlayConfiguration configuration)
        {
            IsInitialized = true;
            Debug.Log("[RewardAdsLevelPlay] Initialize success.");
            _initializeOperation?.TrySetResult(true);
        }

        private void OnInitFailed(LevelPlayInitError error)
        {
            IsInitialized = false;
            var message = $"LevelPlay initialization failed. ErrorCode={error?.ErrorCode}, ErrorMessage={error?.ErrorMessage}";
            Debug.LogError($"[RewardAdsLevelPlay] {message}");
            _initializeOperation?.TrySetException(new InvalidOperationException(message));
        }

        private void OnAdLoaded(string adUnitId, RewardedAdContext context, LevelPlayAdInfo adInfo)
        {
            context.IsReady = true;
            Debug.Log($"[RewardAdsLevelPlay] Load success. AdUnitId={adUnitId}");
            context.LoadOperation?.TrySetResult(true);
        }

        private void OnAdLoadFailed(string adUnitId, RewardedAdContext context, LevelPlayAdError error)
        {
            context.IsReady = false;
            var message = $"LevelPlay load failed. AdUnitId={adUnitId}, ErrorCode={error?.ErrorCode}, ErrorMessage={error?.ErrorMessage}";
            Debug.LogWarning($"[RewardAdsLevelPlay] {message}");
            context.LoadOperation?.TrySetException(new InvalidOperationException(message));
        }

        private void OnAdDisplayFailed(string adUnitId, RewardedAdContext context, LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            context.IsReady = false;
            context.HasReward = false;
            context.HasClosed = false;
            Debug.LogWarning($"[RewardAdsLevelPlay] Show failed. AdUnitId={adUnitId}, ErrorCode={error?.ErrorCode}, ErrorMessage={error?.ErrorMessage}");
            context.ShowOperation?.TrySetResult(RewardedShowResult.Failed);
        }

        private void OnAdRewarded(string adUnitId, RewardedAdContext context, LevelPlayReward reward)
        {
            context.HasReward = true;
            Debug.Log($"[RewardAdsLevelPlay] Reward callback received. AdUnitId={adUnitId}, RewardName={reward?.Name}, RewardAmount={reward?.Amount}");

            if (context.HasClosed && context.ShowOperation != null)
            {
                CompleteShow(adUnitId, context, RewardedShowResult.Completed);
            }
        }

        private void OnAdClosed(string adUnitId, RewardedAdContext context)
        {
            var showOperation = context.ShowOperation;
            if (showOperation == null)
            {
                Debug.Log($"[RewardAdsLevelPlay] Ad closed without active show operation. AdUnitId={adUnitId}");
                return;
            }

            context.HasClosed = true;
            if (context.HasReward)
            {
                CompleteShow(adUnitId, context, RewardedShowResult.Completed);
                return;
            }

            Debug.Log($"[RewardAdsLevelPlay] Ad closed. Waiting {RewardAfterCloseGraceSeconds:0.##}s for reward callback. AdUnitId={adUnitId}");
            CompleteCanceledAfterGraceAsync(adUnitId, context, showOperation).Forget();
        }

        private async UniTaskVoid CompleteCanceledAfterGraceAsync(
            string adUnitId,
            RewardedAdContext context,
            UniTaskCompletionSource<RewardedShowResult> showOperation)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(RewardAfterCloseGraceSeconds));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RewardAdsLevelPlay] Close grace delay failed. AdUnitId={adUnitId}, Exception={ex.Message}");
            }

            if (!ReferenceEquals(context.ShowOperation, showOperation))
            {
                return;
            }

            var result = context.HasReward ? RewardedShowResult.Completed : RewardedShowResult.Canceled;
            CompleteShow(adUnitId, context, result);
        }

        private void CompleteShow(string adUnitId, RewardedAdContext context, RewardedShowResult result)
        {
            context.HasReward = false;
            context.HasClosed = false;
            Debug.Log($"[RewardAdsLevelPlay] Show completed. AdUnitId={adUnitId}, Result={result}");
            context.ShowOperation?.TrySetResult(result);
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

        public UniTask<RewardedShowResult> ShowAsync(
            string adUnitId,
            string rewardIntentId,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException(
                "UNITY_LEVELPLAY scripting define is not enabled. Install/enable the LevelPlay SDK package.");
        }
    }
#endif
}
