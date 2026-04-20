using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Rewards
{
    public sealed class RewardedAdButtonPresenter : MonoBehaviour
    {
        [SerializeField] private RewardedAdButtonView _view;
        [SerializeField] private int _rewardAmount = 50;
        [SerializeField] private string _loadingAdText = "Loading ad...";
        [SerializeField] private string _adUnavailableText = "Ad is unavailable";
        [SerializeField] private string _checkingGrantText = "Checking reward...";
        [SerializeField] private string _grantFailedText = "Failed to grant reward. Please try again";
        [SerializeField] private string _adShowFailedText = "Failed to show ad";

        private AdsRewardFlowService _adsRewardFlowService;
        private CancellationToken _destroyCt;
        private bool _isRequestInProgress;
        private string _lastResultMessage = string.Empty;

        [Inject]
        private void Construct(AdsRewardFlowService adsRewardFlowService)
        {
            _adsRewardFlowService = adsRewardFlowService;
        }

        private void Awake()
        {
            _destroyCt = this.GetCancellationTokenOnDestroy();
        }

        private void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("[RewardAds] RewardedAdButtonView is not assigned.");
                return;
            }

            _view.Clicked += HandleClicked;
            if (_adsRewardFlowService != null)
            {
                _adsRewardFlowService.StateChanged += HandleStateChanged;
                InitializeAsync(_destroyCt).Forget();
            }
            else
            {
                _view.SetStatus(_adUnavailableText);
                _view.SetInteractable(false);
            }
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.Clicked -= HandleClicked;
            }

            if (_adsRewardFlowService != null)
            {
                _adsRewardFlowService.StateChanged -= HandleStateChanged;
            }
        }

        private async UniTaskVoid InitializeAsync(CancellationToken ct)
        {
            try
            {
                await _adsRewardFlowService.InitializeAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[RewardAds] Initial ads setup failed from presenter. {exception.Message}");
                _lastResultMessage = _adUnavailableText;
            }
            finally
            {
                UpdateViewFromState(_adsRewardFlowService.State);
            }
        }

        private void HandleStateChanged(RewardAdFlowState state)
        {
            UpdateViewFromState(state);
        }

        private void HandleClicked()
        {
            if (_isRequestInProgress || _adsRewardFlowService == null)
            {
                return;
            }

            RunFlowAsync(_destroyCt).Forget();
        }

        private async UniTaskVoid RunFlowAsync(CancellationToken ct)
        {
            _isRequestInProgress = true;
            UpdateViewFromState(_adsRewardFlowService.State);

            try
            {
                var result = await _adsRewardFlowService.TryRunFlowAsync(ct);
                _lastResultMessage = BuildMessageFromResult(result);
            }
            catch (OperationCanceledException)
            {
                _lastResultMessage = string.Empty;
            }
            finally
            {
                _isRequestInProgress = false;
                UpdateViewFromState(_adsRewardFlowService.State);
            }
        }

        private string BuildMessageFromResult(RewardGrantFlowResult result)
        {
            if (result == null)
            {
                return _grantFailedText;
            }

            return result.Type switch
            {
                RewardGrantFlowResultType.Success => $"Received {_rewardAmount} crystals",
                RewardGrantFlowResultType.AdNotReady => _adUnavailableText,
                RewardGrantFlowResultType.AdCanceled => string.Empty,
                RewardGrantFlowResultType.AdFailed => _adShowFailedText,
                RewardGrantFlowResultType.ServerFailed => _grantFailedText,
                RewardGrantFlowResultType.NetworkError => _grantFailedText,
                _ => _grantFailedText
            };
        }

        private void UpdateViewFromState(RewardAdFlowState state)
        {
            if (_view == null || _adsRewardFlowService == null)
            {
                return;
            }

            var isLoading =
                state == RewardAdFlowState.InitializingAds ||
                state == RewardAdFlowState.LoadingAd ||
                state == RewardAdFlowState.ShowingAd ||
                state == RewardAdFlowState.WaitingServerGrant;

            _view.SetLoading(isLoading);
            _view.SetInteractable(_adsRewardFlowService.IsReady && !_isRequestInProgress);

            var status = state switch
            {
                RewardAdFlowState.InitializingAds => _loadingAdText,
                RewardAdFlowState.LoadingAd => _loadingAdText,
                RewardAdFlowState.WaitingServerGrant => _checkingGrantText,
                RewardAdFlowState.Failed => string.IsNullOrWhiteSpace(_lastResultMessage) ? _adUnavailableText : _lastResultMessage,
                RewardAdFlowState.Success => string.IsNullOrWhiteSpace(_lastResultMessage)
                    ? $"Received {_rewardAmount} crystals"
                    : _lastResultMessage,
                RewardAdFlowState.Ready => _lastResultMessage,
                RewardAdFlowState.Idle => _lastResultMessage,
                _ => _lastResultMessage
            };

            _view.SetStatus(status);
        }
    }
}
