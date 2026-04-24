using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace Rewards
{
    public sealed class AdsRewardFlowService
    {
        private const int PreloadRetryDelaySeconds = 2;
        private const int DefaultLegacyGrantTimeoutSeconds = 15;
        private const int DefaultGrantConfirmationTimeoutSeconds = 20;
        private const float DefaultGrantPollingIntervalSeconds = 1f;

        private readonly IRewardedAdsProvider _adsProvider;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly IRewardIntentService _rewardIntentService;
        private readonly IRewardPlayerStateSyncService _rewardPlayerStateSyncService;
        private readonly RewardedAdsConfig _config;
        private readonly SemaphoreSlim _initializeGate = new(1, 1);

        private RewardAdFlowState _state = RewardAdFlowState.Idle;
        private int _isFlowInProgress;

        public AdsRewardFlowService(
            IRewardedAdsProvider adsProvider,
            IRewardGrantService rewardGrantService,
            IRewardIntentService rewardIntentService,
            IRewardPlayerStateSyncService rewardPlayerStateSyncService,
            RewardedAdsConfigSO configSo)
        {
            _adsProvider = adsProvider ?? throw new ArgumentNullException(nameof(adsProvider));
            _rewardGrantService = rewardGrantService ?? throw new ArgumentNullException(nameof(rewardGrantService));
            _rewardIntentService = rewardIntentService ?? throw new ArgumentNullException(nameof(rewardIntentService));
            _rewardPlayerStateSyncService = rewardPlayerStateSyncService ?? throw new ArgumentNullException(nameof(rewardPlayerStateSyncService));
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

                Debug.Log("[AdsRewardFlow] Ads init started.");
                SetState(RewardAdFlowState.InitializingAds);
                await _adsProvider.InitializeAsync(ct);
                Debug.Log("[AdsRewardFlow] Ads init success.");

                var preloadSuccess = await TryPreloadWithRetryAsync(adUnitId, ct);
                SetState(preloadSuccess ? RewardAdFlowState.Ready : RewardAdFlowState.Failed);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AdsRewardFlow] Ads init failed. {exception.Message}");
                SetState(RewardAdFlowState.Failed);
                throw;
            }
            finally
            {
                _initializeGate.Release();
            }
        }

        public UniTask<RewardGrantFlowResult> TryRunFlowAsync(CancellationToken ct = default)
        {
            return TryRunFlowCoreAsync(_config.RewardId, ct);
        }

        public UniTask<RewardGrantFlowResult> TryRunFlowForRewardAsync(string rewardId, CancellationToken ct = default)
        {
            return TryRunFlowCoreAsync(rewardId, ct);
        }

        private async UniTask<RewardGrantFlowResult> TryRunFlowCoreAsync(string rewardId, CancellationToken ct)
        {
            if (Interlocked.CompareExchange(ref _isFlowInProgress, 1, 0) != 0)
            {
                return RewardGrantFlowResult.Build(
                    RewardGrantFlowResultType.UnknownError,
                    errorCode: "FLOW_IN_PROGRESS",
                    errorMessage: "Reward flow is already running.");
            }

            var showWasAttempted = false;
            RewardGrantFlowResult finalResult = null;

            try
            {
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    SetState(RewardAdFlowState.Failed);
                    finalResult = RewardGrantFlowResult.Build(
                        RewardGrantFlowResultType.ServerFailed,
                        errorCode: "EMPTY_REWARD_ID",
                        errorMessage: "Reward id is empty.");
                    return finalResult;
                }

                await EnsureInitializedAsync(ct);

                var adUnitId = GetAdUnitId();
                if (!_adsProvider.IsAdReady(adUnitId))
                {
                    Debug.LogWarning($"[AdsRewardFlow] Ad is not ready. AdUnitId={adUnitId}");
                    finalResult = RewardGrantFlowResult.Build(
                        RewardGrantFlowResultType.AdNotReady,
                        errorCode: "AD_NOT_READY",
                        errorMessage: "Ad is not ready.");
                    return finalResult;
                }

                if (_config.UseServerConfirmedGrantFlow)
                {
                    Debug.Log($"[AdsRewardFlow] Intent create started. RewardId={rewardId}");
                    var createResult = await _rewardIntentService.CreateAsync(rewardId, ct);
                    if (!createResult.IsSuccess || string.IsNullOrWhiteSpace(createResult.RewardIntentId))
                    {
                        SetState(RewardAdFlowState.Failed);
                        var isNetworkFailure = IsNetworkCreateFailure(createResult);
                        var errorCode = isNetworkFailure ? "INTENT_CREATE_NETWORK_ERROR" : "INTENT_CREATE_FAILED";
                        Debug.LogWarning(
                            $"[AdsRewardFlow] Intent create failed. Code={createResult.ErrorCode}, Message={createResult.ErrorMessage}, MappedCode={errorCode}");
                        finalResult = RewardGrantFlowResult.Build(
                            isNetworkFailure ? RewardGrantFlowResultType.NetworkError : RewardGrantFlowResultType.ServerFailed,
                            errorCode: errorCode,
                            errorMessage: createResult.ErrorMessage);
                        return finalResult;
                    }

                    var rewardIntentId = createResult.RewardIntentId;
                    Debug.Log($"[AdsRewardFlow] Intent create success. RewardIntentId={rewardIntentId}");

                    SetState(RewardAdFlowState.ShowingAd);
                    Debug.Log($"[AdsRewardFlow] Ad show started. AdUnitId={adUnitId}, RewardIntentId={rewardIntentId}");
                    showWasAttempted = true;
                    var showResult = await _adsProvider.ShowAsync(adUnitId, rewardIntentId, ct);
                    switch (showResult)
                    {
                        case RewardedShowResult.Completed:
                            Debug.Log("[AdsRewardFlow] Ad show completed.");
                            finalResult = await WaitForIntentConfirmationAsync(rewardIntentId, ct);
                            return finalResult;

                        case RewardedShowResult.Canceled:
                            Debug.Log("[AdsRewardFlow] Ad show canceled.");
                            finalResult = RewardGrantFlowResult.Build(RewardGrantFlowResultType.AdCanceled);
                            return finalResult;

                        case RewardedShowResult.Failed:
                        default:
                            Debug.LogWarning("[AdsRewardFlow] Ad show failed.");
                            SetState(RewardAdFlowState.Failed);
                            finalResult = RewardGrantFlowResult.Build(
                                RewardGrantFlowResultType.AdFailed,
                                errorCode: "AD_SHOW_FAILED",
                                errorMessage: "Failed to show ad.");
                            return finalResult;
                    }
                }

                SetState(RewardAdFlowState.ShowingAd);
                Debug.Log($"[AdsRewardFlow] Ad show started (legacy). AdUnitId={adUnitId}");
                showWasAttempted = true;
                var legacyShowResult = await _adsProvider.ShowAsync(adUnitId, string.Empty, ct);
                switch (legacyShowResult)
                {
                    case RewardedShowResult.Completed:
                        Debug.Log("[AdsRewardFlow] Ad show completed (legacy).");
                        finalResult = await ExecuteLegacyGrantRequestAsync(rewardId, ct);
                        return finalResult;

                    case RewardedShowResult.Canceled:
                        Debug.Log("[AdsRewardFlow] Ad show canceled (legacy).");
                        finalResult = RewardGrantFlowResult.Build(RewardGrantFlowResultType.AdCanceled);
                        return finalResult;

                    case RewardedShowResult.Failed:
                    default:
                        Debug.LogWarning("[AdsRewardFlow] Ad show failed (legacy).");
                        SetState(RewardAdFlowState.Failed);
                        finalResult = RewardGrantFlowResult.Build(
                            RewardGrantFlowResultType.AdFailed,
                            errorCode: "AD_SHOW_FAILED",
                            errorMessage: "Failed to show ad.");
                        return finalResult;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[AdsRewardFlow] Flow failed. {exception.Message}");
                SetState(RewardAdFlowState.Failed);
                finalResult = RewardGrantFlowResult.Build(
                    RewardGrantFlowResultType.UnknownError,
                    errorCode: "FLOW_ERROR",
                    errorMessage: exception.Message);
                return finalResult;
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

                if (finalResult != null)
                {
                    Debug.Log($"[AdsRewardFlow] Final flow result. Type={finalResult.Type}, ErrorCode={finalResult.ErrorCode}, ErrorMessage={finalResult.ErrorMessage}");
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

        private async UniTask<RewardGrantFlowResult> ExecuteLegacyGrantRequestAsync(string rewardId, CancellationToken ct)
        {
            SetState(RewardAdFlowState.WaitingServerGrant);
            Debug.Log($"[AdsRewardFlow] Legacy grant request started. RewardId={rewardId}");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.GetGrantTimeoutSecondsOrDefault(DefaultLegacyGrantTimeoutSeconds)));

            try
            {
                var grantResult = await _rewardGrantService.TryGrantDetailedAsync(rewardId, timeoutCts.Token);
                if (grantResult.Success)
                {
                    Debug.Log("[AdsRewardFlow] Legacy grant request success.");
                    SetState(RewardAdFlowState.Success);
                    return RewardGrantFlowResult.Build(RewardGrantFlowResultType.Success);
                }

                Debug.LogWarning(
                    $"[AdsRewardFlow] Legacy grant request failed. FailureType={grantResult.FailureType}, Code={grantResult.ErrorCode}, Message={grantResult.ErrorMessage}");

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
                Debug.LogWarning("[AdsRewardFlow] Legacy grant request timed out.");
                SetState(RewardAdFlowState.Failed);
                return RewardGrantFlowResult.Build(
                    RewardGrantFlowResultType.NetworkError,
                    errorCode: "TIMEOUT",
                    errorMessage: "Grant request timed out.");
            }
        }

        private async UniTask<RewardGrantFlowResult> WaitForIntentConfirmationAsync(
            string rewardIntentId,
            CancellationToken ct)
        {
            SetState(RewardAdFlowState.WaitingServerGrant);
            Debug.Log($"[AdsRewardFlow] Waiting for reward confirmation started. RewardIntentId={rewardIntentId}");

            var timeoutSeconds = _config.GetGrantConfirmationTimeoutSecondsOrDefault(DefaultGrantConfirmationTimeoutSeconds);
            var pollingIntervalSeconds = _config.GetGrantPollingIntervalSecondsOrDefault(DefaultGrantPollingIntervalSeconds);
            var hadNetworkError = false;
            var receivedAnyStatus = false;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                while (true)
                {
                    timeoutCts.Token.ThrowIfCancellationRequested();

                    try
                    {
                        GetRewardIntentStatusResult statusResult = await _rewardIntentService.GetStatusAsync(rewardIntentId, timeoutCts.Token);
                        receivedAnyStatus = true;
                        var status = statusResult?.Status ?? RewardIntentStatus.Unknown;

                        if (_config.EnableIntentPollingLogs || status != RewardIntentStatus.Pending)
                        {
                            Debug.Log($"[AdsRewardFlow] Intent status received. RewardIntentId={rewardIntentId}, Status={status}, ErrorCode={statusResult?.ErrorCode}");
                        }

                        switch (status)
                        {
                            case RewardIntentStatus.Fulfilled:
                                try
                                {
                                    Debug.Log($"[AdsRewardFlow] Intent fulfilled. Starting save/global sync. RewardIntentId={rewardIntentId}");

                                    await _rewardPlayerStateSyncService.SyncFromGlobalSaveAsync(timeoutCts.Token);

                                    SetState(RewardAdFlowState.Success);
                                    return RewardGrantFlowResult.Build(RewardGrantFlowResultType.Success);
                                }
                                catch (OperationCanceledException)
                                {
                                    throw;
                                }
                                catch (WebClientNetworkException exception)
                                {
                                    SetState(RewardAdFlowState.Failed);
                                    Debug.LogWarning(
                                        $"[AdsRewardFlow] Save/global sync network failure. RewardIntentId={rewardIntentId}, Reason={exception.Message}");
                                    return RewardGrantFlowResult.Build(
                                        RewardGrantFlowResultType.NetworkError,
                                        errorCode: "SAVE_SYNC_NETWORK_ERROR",
                                        errorMessage: exception.Message);
                                }
                                catch (Exception exception)
                                {
                                    SetState(RewardAdFlowState.Failed);
                                    Debug.LogWarning(
                                        $"[AdsRewardFlow] Save/global sync failed. RewardIntentId={rewardIntentId}, Reason={exception.Message}");
                                    return RewardGrantFlowResult.Build(
                                        RewardGrantFlowResultType.ServerFailed,
                                        errorCode: "SAVE_SYNC_FAILED",
                                        errorMessage: exception.Message);
                                }

                            case RewardIntentStatus.Rejected:
                                SetState(RewardAdFlowState.Failed);
                                return RewardGrantFlowResult.Build(
                                    RewardGrantFlowResultType.ServerFailed,
                                    errorCode: "REWARD_REJECTED",
                                    errorMessage: statusResult?.ErrorMessage);

                            case RewardIntentStatus.Expired:
                                SetState(RewardAdFlowState.Failed);
                                return RewardGrantFlowResult.Build(
                                    RewardGrantFlowResultType.ServerFailed,
                                    errorCode: "REWARD_EXPIRED",
                                    errorMessage: statusResult?.ErrorMessage);

                            case RewardIntentStatus.Failed:
                                SetState(RewardAdFlowState.Failed);
                                return RewardGrantFlowResult.Build(
                                    RewardGrantFlowResultType.ServerFailed,
                                    errorCode: "REWARD_CONFIRM_FAILED",
                                    errorMessage: statusResult?.ErrorMessage);

                            case RewardIntentStatus.Pending:
                            case RewardIntentStatus.Unknown:
                            default:
                                break;
                        }
                    }
                    catch (WebClientNetworkException exception)
                    {
                        hadNetworkError = true;
                        Debug.LogWarning($"[AdsRewardFlow] Reward confirmation polling network error. RewardIntentId={rewardIntentId}, Reason={exception.Message}");
                    }
                    catch (WebClientException exception)
                    {
                        hadNetworkError = true;
                        Debug.LogWarning($"[AdsRewardFlow] Reward confirmation polling transient error. RewardIntentId={rewardIntentId}, Reason={exception.Message}");
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), cancellationToken: timeoutCts.Token);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                SetState(RewardAdFlowState.Failed);
                var timeoutErrorCode = hadNetworkError && !receivedAnyStatus
                    ? "REWARD_CONFIRM_NETWORK_ERROR"
                    : "REWARD_CONFIRM_TIMEOUT";
                Debug.LogWarning($"[AdsRewardFlow] Reward confirmation wait ended by timeout. RewardIntentId={rewardIntentId}, ErrorCode={timeoutErrorCode}");
                return RewardGrantFlowResult.Build(
                    RewardGrantFlowResultType.ServerFailed,
                    errorCode: timeoutErrorCode,
                    errorMessage: "Reward confirmation timed out.");
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
            Debug.Log($"[AdsRewardFlow] Ad load started. AdUnitId={adUnitId}");
            try
            {
                await _adsProvider.PreloadAsync(adUnitId, ct);
                Debug.Log($"[AdsRewardFlow] Ad load success. AdUnitId={adUnitId}");
                return _adsProvider.IsAdReady(adUnitId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception firstException)
            {
                Debug.LogWarning($"[AdsRewardFlow] Ad load failed. AdUnitId={adUnitId}, Reason={firstException.Message}");
            }

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(PreloadRetryDelaySeconds), cancellationToken: ct);
                Debug.Log($"[AdsRewardFlow] Ad load retry started. AdUnitId={adUnitId}");
                await _adsProvider.PreloadAsync(adUnitId, ct);
                var ready = _adsProvider.IsAdReady(adUnitId);
                Debug.Log(ready
                    ? $"[AdsRewardFlow] Ad load retry success. AdUnitId={adUnitId}"
                    : $"[AdsRewardFlow] Ad load retry completed but ad is not ready. AdUnitId={adUnitId}");
                return ready;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception secondException)
            {
                Debug.LogError($"[AdsRewardFlow] Ad load retry failed. AdUnitId={adUnitId}, Reason={secondException.Message}");
                return false;
            }
        }

        private static bool IsNetworkCreateFailure(CreateRewardIntentResult createResult)
        {
            if (createResult == null)
            {
                return false;
            }

            return string.Equals(createResult.ErrorCode, "NETWORK_ERROR", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(createResult.ErrorCode, "TIMEOUT", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(createResult.ErrorCode, "REWARD_CONFIRM_NETWORK_ERROR", StringComparison.OrdinalIgnoreCase);
        }

        private string GetAdUnitId()
        {
            return _config.GetLevelPlayRewardedAdUnitIdForCurrentPlatform();
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
