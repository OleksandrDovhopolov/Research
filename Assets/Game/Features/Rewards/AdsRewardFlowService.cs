using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rewards
{
    public sealed class AdsRewardFlowService
    {
        private const int PreloadRetryDelaySeconds = 2;
        private const int DefaultGrantTimeoutSeconds = 15;

        private readonly IRewardedAdsProvider _adsProvider;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly RewardedAdsConfig _config;
        private readonly SemaphoreSlim _initializeGate = new(1, 1);

        private RewardAdFlowState _state = RewardAdFlowState.Idle;
        private int _isFlowInProgress;

        public AdsRewardFlowService(
            IRewardedAdsProvider adsProvider,
            IRewardGrantService rewardGrantService,
            RewardedAdsConfigSO configSo)
        {
            _adsProvider = adsProvider ?? throw new ArgumentNullException(nameof(adsProvider));
            _rewardGrantService = rewardGrantService ?? throw new ArgumentNullException(nameof(rewardGrantService));
            _config = (configSo ?? throw new ArgumentNullException(nameof(configSo))).GetOrCreate();
        }

        public event Action<RewardAdFlowState> StateChanged;

        public RewardAdFlowState State => _state;

        public bool IsFlowInProgress => Interlocked.CompareExchange(ref _isFlowInProgress, 0, 0) == 1;

        public bool IsReady =>
            _adsProvider.IsInitialized &&
            !string.IsNullOrWhiteSpace(GetAdUnitId()) &&
            _adsProvider.IsAdReady(GetAdUnitId()) &&
            !IsFlowInProgress;

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await _initializeGate.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                var adUnitId = GetAdUnitId();
                if (string.IsNullOrWhiteSpace(adUnitId))
                {
                    SetState(RewardAdFlowState.Failed);
                    throw new InvalidOperationException("Rewarded ad unit id is not configured for the current platform.");
                }

                Debug.Log("[RewardAds] Ads init started.");
                SetState(RewardAdFlowState.InitializingAds);
                await _adsProvider.InitializeAsync(ct);
                Debug.Log("[RewardAds] Ads init success.");

                var preloadSuccess = await TryPreloadWithRetryAsync(adUnitId, ct);
                SetState(preloadSuccess ? RewardAdFlowState.Ready : RewardAdFlowState.Failed);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[RewardAds] Ads init failed. {exception.Message}");
                SetState(RewardAdFlowState.Failed);
                throw;
            }
            finally
            {
                _initializeGate.Release();
            }
        }

        public async UniTask<RewardGrantFlowResult> TryRunFlowAsync(CancellationToken ct = default)
        {
            if (Interlocked.CompareExchange(ref _isFlowInProgress, 1, 0) != 0)
            {
                return RewardGrantFlowResult.Build(
                    RewardGrantFlowResultType.UnknownError,
                    errorCode: "FLOW_IN_PROGRESS",
                    errorMessage: "Reward flow is already running.");
            }

            var showWasAttempted = false;
            try
            {
                await EnsureInitializedAsync(ct);

                var adUnitId = GetAdUnitId();
                if (!_adsProvider.IsAdReady(adUnitId))
                {
                    Debug.LogWarning($"[RewardAds] Ad is not ready. AdUnitId={adUnitId}");
                    return RewardGrantFlowResult.Build(
                        RewardGrantFlowResultType.AdNotReady,
                        errorCode: "AD_NOT_READY",
                        errorMessage: "Ad is not ready.");
                }

                SetState(RewardAdFlowState.ShowingAd);
                Debug.Log("[RewardAds] Ad show started.");
                showWasAttempted = true;
                var showResult = await _adsProvider.ShowAsync(adUnitId, ct);
                switch (showResult)
                {
                    case RewardedShowResult.Completed:
                        Debug.Log("[RewardAds] Ad show completed.");
                        return await ExecuteGrantRequestAsync(ct);

                    case RewardedShowResult.Canceled:
                        Debug.Log("[RewardAds] Ad show canceled.");
                        return RewardGrantFlowResult.Build(RewardGrantFlowResultType.AdCanceled);

                    case RewardedShowResult.Failed:
                    default:
                        Debug.LogWarning("[RewardAds] Ad show failed.");
                        SetState(RewardAdFlowState.Failed);
                        return RewardGrantFlowResult.Build(
                            RewardGrantFlowResultType.AdFailed,
                            errorCode: "AD_SHOW_FAILED",
                            errorMessage: "Failed to show ad.");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[RewardAds] Flow failed. {exception.Message}");
                SetState(RewardAdFlowState.Failed);
                return RewardGrantFlowResult.Build(
                    RewardGrantFlowResultType.UnknownError,
                    errorCode: "FLOW_ERROR",
                    errorMessage: exception.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _isFlowInProgress, 0);
                if (showWasAttempted)
                {
                    await ReloadAfterShowAsync();
                }
                else if (State == RewardAdFlowState.Success)
                {
                    SetState(RewardAdFlowState.Ready);
                }
            }
        }

        private async UniTask EnsureInitializedAsync(CancellationToken ct)
        {
            if (_adsProvider.IsInitialized && _adsProvider.IsAdReady(GetAdUnitId()))
            {
                SetState(RewardAdFlowState.Ready);
                return;
            }

            await InitializeAsync(ct);
        }

        private async UniTask<RewardGrantFlowResult> ExecuteGrantRequestAsync(CancellationToken ct)
        {
            SetState(RewardAdFlowState.WaitingServerGrant);
            Debug.Log($"[RewardAds] Grant request started. RewardId={_config.RewardId}");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.GetGrantTimeoutSecondsOrDefault(DefaultGrantTimeoutSeconds)));

            try
            {
                RewardGrantDetailedResult grantResult = await _rewardGrantService.TryGrantDetailedAsync(_config.RewardId, timeoutCts.Token);
                if (grantResult.Success)
                {
                    Debug.Log("[RewardAds] Grant request success.");
                    SetState(RewardAdFlowState.Success);
                    return RewardGrantFlowResult.Build(
                        RewardGrantFlowResultType.Success,
                        newCrystalsBalance: grantResult.NewCrystalsBalance);
                }

                Debug.LogWarning(
                    $"[RewardAds] Grant request failed. FailureType={grantResult.FailureType}, Code={grantResult.ErrorCode}, Message={grantResult.ErrorMessage}");

                SetState(RewardAdFlowState.Failed);
                return grantResult.FailureType == RewardGrantFailureType.Network
                    ? RewardGrantFlowResult.Build(
                        RewardGrantFlowResultType.NetworkError,
                        errorCode: grantResult.ErrorCode,
                        errorMessage: grantResult.ErrorMessage)
                    : RewardGrantFlowResult.Build(
                        RewardGrantFlowResultType.ServerFailed,
                        errorCode: grantResult.ErrorCode,
                        errorMessage: grantResult.ErrorMessage);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                Debug.LogWarning("[RewardAds] Grant request timed out.");
                SetState(RewardAdFlowState.Failed);
                return RewardGrantFlowResult.Build(
                    RewardGrantFlowResultType.NetworkError,
                    errorCode: "TIMEOUT",
                    errorMessage: "Grant request timed out.");
            }
        }

        private async UniTask ReloadAfterShowAsync()
        {
            var adUnitId = GetAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId) || !_adsProvider.IsInitialized)
            {
                SetState(RewardAdFlowState.Idle);
                return;
            }

            var preloaded = await TryPreloadWithRetryAsync(adUnitId, CancellationToken.None);
            SetState(preloaded ? RewardAdFlowState.Ready : RewardAdFlowState.Idle);
        }

        private async UniTask<bool> TryPreloadWithRetryAsync(string adUnitId, CancellationToken ct)
        {
            SetState(RewardAdFlowState.LoadingAd);
            Debug.Log($"[RewardAds] Ad load started. AdUnitId={adUnitId}");
            try
            {
                await _adsProvider.PreloadAsync(adUnitId, ct);
                Debug.Log($"[RewardAds] Ad load success. AdUnitId={adUnitId}");
                return _adsProvider.IsAdReady(adUnitId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception firstException)
            {
                Debug.LogWarning($"[RewardAds] Ad load failed. AdUnitId={adUnitId}, Reason={firstException.Message}");
            }

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(PreloadRetryDelaySeconds), cancellationToken: ct);
                Debug.Log($"[RewardAds] Ad load retry started. AdUnitId={adUnitId}");
                await _adsProvider.PreloadAsync(adUnitId, ct);
                var ready = _adsProvider.IsAdReady(adUnitId);
                Debug.Log(ready
                    ? $"[RewardAds] Ad load retry success. AdUnitId={adUnitId}"
                    : $"[RewardAds] Ad load retry completed but ad is not ready. AdUnitId={adUnitId}");
                return ready;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception secondException)
            {
                Debug.LogError($"[RewardAds] Ad load retry failed. AdUnitId={adUnitId}, Reason={secondException.Message}");
                return false;
            }
        }

        private string GetAdUnitId()
        {
            return _config.GetRewardedAdUnitIdForCurrentPlatform();
        }

        private void SetState(RewardAdFlowState state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            StateChanged?.Invoke(_state);
        }
    }
}
