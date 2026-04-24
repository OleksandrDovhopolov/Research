using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Rewards;
using UISystem;
using UnityEngine;
using VContainer;

namespace FortuneWheel
{
    public sealed class FortuneWheelSectorArgs
    {
        public string RewardId { get; }
        public Sprite RewardIcon { get; }
        public int RewardAmount { get; }

        public FortuneWheelSectorArgs(string rewardId, Sprite rewardIcon, int rewardAmount)
        {
            RewardId = rewardId;
            RewardIcon = rewardIcon;
            RewardAmount = rewardAmount;
        }
    }

    public class FortuneWheelArgs : WindowArgs
    {
        public FortuneWheelDataServerItem InitialData { get; }
        public IReadOnlyList<FortuneWheelSectorArgs> Sectors { get; }

        public FortuneWheelArgs(FortuneWheelDataServerItem initialData, IReadOnlyList<FortuneWheelSectorArgs> sectors)
        {
            InitialData = initialData;
            Sectors = sectors;
        }
    }

    [Window("FortuneWheelWindow")]
    public class FortuneWheelController : WindowController<FortuneWheelView>
    {
        private IFortuneWheelServerService _fortuneWheelServerService;
        private IFortuneWheelTimerService _fortuneWheelTimerService;
        private FortuneWheelAdSpinOrchestrator _fortuneWheelAdSpinOrchestrator;

        private FortuneWheelArgs Args => Arguments as FortuneWheelArgs;
        private IReadOnlyList<FortuneWheelSectorArgs> Sectors => Args?.Sectors ?? Array.Empty<FortuneWheelSectorArgs>();

        private int _currentSpinsAmount;
        private bool _isAdSpinAvailable;
        private bool _isSpinning;
        private bool _isSpinRequestInProgress;
        private int _adSpinFlowInProgressCount;
        private bool _isDataValid;
        private CancellationTokenSource _requestCts;
        private TimeSpan _currentRemainingTime;
        private FortuneWheelSpinResult _lastSuccessfulSpinResult;

        private bool IsAdSpinFlowInProgress => _adSpinFlowInProgressCount > 0;

        public override bool IsCloseBlocked => _isSpinning;

        [Inject]
        private void Construct(
            IFortuneWheelServerService fortuneWheelServerService,
            IFortuneWheelTimerService fortuneWheelTimerService,
            FortuneWheelAdSpinOrchestrator fortuneWheelAdSpinOrchestrator)
        {
            _fortuneWheelServerService = fortuneWheelServerService;
            _fortuneWheelTimerService = fortuneWheelTimerService;
            _fortuneWheelAdSpinOrchestrator = fortuneWheelAdSpinOrchestrator;
        }

        protected override void OnShowStart()
        {
            ResetRequestCts();

            var args = Args;
            if (args == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelController)}] {nameof(FortuneWheelArgs)} are missing.");
                _isDataValid = false;
                _currentSpinsAmount = 0;
                _isAdSpinAvailable = false;
                _currentRemainingTime = TimeSpan.Zero;
                RefreshViewData();
                UpdateInteractionState();
                return;
            }

            if (args.InitialData == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelController)}] Initial wheel data is missing.");
                _isDataValid = false;
                _currentSpinsAmount = 0;
                _isAdSpinAvailable = false;
                _currentRemainingTime = TimeSpan.Zero;
                View.SetData(args);
                RefreshViewData();
                UpdateInteractionState();
                return;
            }

            _currentSpinsAmount = Mathf.Max(0, args.InitialData.AvailableSpins);
            _isAdSpinAvailable = args.InitialData.AdSpinAvailable;
            _currentRemainingTime = CalculateRemainingTime(args.InitialData.NextUpdateAt);
            _isDataValid = ValidateSectors(Sectors);

            View.SetData(args);
            RefreshViewData();
            SubscribeTimerService();
            StartTimerService(args.InitialData);
            UpdateInteractionState();
        }

        protected override void OnShowComplete()
        {
            View.CloseClick += CloseWindow;
            View.SpinClick += OnSpinClicked;
            View.SpinAdClick += OnSpinAdClicked;
        }

        protected override void OnHideStart(bool isClosed)
        {
            View.SpinAdClick -= OnSpinAdClicked;
            View.SpinClick -= OnSpinClicked;
            View.CloseClick -= CloseWindow;
            UnsubscribeTimerService();
            _fortuneWheelTimerService?.Stop();
            CancelRequests();
            CancelSpin();
        }

        private void OnSpinClicked()
        {
            OnSpinClickedAsync(_requestCts?.Token ?? CancellationToken.None).Forget();
        }

        private void OnSpinAdClicked()
        {
            OnSpinAdClickedAsync(_requestCts?.Token ?? CancellationToken.None).Forget();
        }

        private async UniTaskVoid OnSpinClickedAsync(CancellationToken ct)
        {
            if (_isSpinning || _isSpinRequestInProgress || IsAdSpinFlowInProgress || !_isDataValid || _currentSpinsAmount <= 0)
            {
                return;
            }

            await ExecuteSpinRequestAsync(ct, useOptimisticSpinDecrement: true);
        }

        private async UniTaskVoid OnSpinAdClickedAsync(CancellationToken ct)
        {
            if (_isSpinning || !_isDataValid)
            {
                return;
            }

            if (_fortuneWheelAdSpinOrchestrator == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelController)}] {nameof(FortuneWheelAdSpinOrchestrator)} is not available.");
                return;
            }

            _adSpinFlowInProgressCount++;
            UpdateInteractionState();

            try
            {
                var adFlowResult = await _fortuneWheelAdSpinOrchestrator.TryRunFlowAsync(ct);
                if (adFlowResult == null || adFlowResult.Type != RewardGrantFlowResultType.Success)
                {
                    return;
                }

                if (_isSpinning || _isSpinRequestInProgress || !_isDataValid)
                {
                    return;
                }

                await ExecuteSpinRequestAsync(ct, useOptimisticSpinDecrement: false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(FortuneWheelController)}] Ad spin flow failed. {exception.Message}");
            }
            finally
            {
                _adSpinFlowInProgressCount = Math.Max(0, _adSpinFlowInProgressCount - 1);
                if (!_isSpinning && !_isSpinRequestInProgress)
                {
                    UpdateInteractionState();
                }
            }
        }

        private async UniTask ExecuteSpinRequestAsync(CancellationToken ct, bool useOptimisticSpinDecrement)
        {
            if (_fortuneWheelServerService == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelController)}] {nameof(IFortuneWheelServerService)} is not available.");
                return;
            }

            var previousSpinsAmount = _currentSpinsAmount;
            if (useOptimisticSpinDecrement)
            {
                _currentSpinsAmount = Mathf.Max(0, _currentSpinsAmount - 1);
                RefreshViewData();
            }

            _isSpinRequestInProgress = true;
            UpdateInteractionState();

            try
            {
                var spinResult = await _fortuneWheelServerService.SpinAsync(ct);
                Debug.LogWarning($"[{GetType().Name}] spinResult with RewardId {spinResult.RewardId}");
                ApplySpinResult(spinResult);
                _lastSuccessfulSpinResult = spinResult;

                var targetSectorIndex = FindSectorIndexByRewardId(Sectors, spinResult.RewardId);
                if (targetSectorIndex < 0)
                {
                    Debug.LogWarning($"[{nameof(FortuneWheelController)}] Reward id '{spinResult.RewardId}' was not found in sectors.");
                    OnSpinCompleted();
                    return;
                }

                _isSpinning = true;
                UpdateInteractionState();

                var animationStarted = View.PlaySpinToSector(targetSectorIndex, OnSpinCompleted);
                if (!animationStarted)
                {
                    _isSpinning = false;
                    UpdateInteractionState();
                    OnSpinCompleted();
                }
            }
            catch (OperationCanceledException)
            {
                if (useOptimisticSpinDecrement)
                {
                    RestoreOptimisticSpins(previousSpinsAmount);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(FortuneWheelController)}] Spin request failed. {exception.Message}");
                if (useOptimisticSpinDecrement)
                {
                    RestoreOptimisticSpins(previousSpinsAmount);
                }
            }
            finally
            {
                _isSpinRequestInProgress = false;
                if (!_isSpinning)
                {
                    UpdateInteractionState();
                }
            }
        }

        private void OnSpinCompleted()
        {
            var spinResult = _lastSuccessfulSpinResult;
            _lastSuccessfulSpinResult = null;
            if (spinResult == null)
            {
                return;
            }

            _isSpinning = false;
            UpdateInteractionState();

            ShowRewardWindow(spinResult);
        }

        private void UpdateInteractionState()
        {
            var regularSpinIsBusy = !_isDataValid || _isSpinning || _isSpinRequestInProgress || IsAdSpinFlowInProgress;
            View.SetSpinButtonInteractable(!regularSpinIsBusy && _currentSpinsAmount > 0);
            View.SetSpinAdButtonInteractable(_isDataValid && !_isSpinning);
            View.SetCloseInteractable(_isSpinning);
        }

        private void CancelSpin()
        {
            _isSpinning = false;
            _isSpinRequestInProgress = false;
            _adSpinFlowInProgressCount = 0;
            _lastSuccessfulSpinResult = null;
            View.StopSpinAnimation();
            UpdateInteractionState();
        }

        private void CancelRequests()
        {
            if (_requestCts == null)
            {
                return;
            }

            _requestCts.Cancel();
            _requestCts.Dispose();
            _requestCts = null;
        }

        private void ResetRequestCts()
        {
            CancelRequests();
            _requestCts = new CancellationTokenSource();
        }

        private void RefreshViewData()
        {
            View.SetSpinsAmount(_currentSpinsAmount);
            View.SetRemainingTime(_currentRemainingTime);
        }

        private void ApplySpinResult(FortuneWheelSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return;
            }

            if (_fortuneWheelTimerService != null)
            {
                _fortuneWheelTimerService.ApplySpinResult(spinResult);
                return;
            }

            ApplyStateUpdate(new FortuneWheelDataServerItem(
                spinResult.AvailableSpins,
                spinResult.UpdatedAt,
                spinResult.NextUpdateAt,
                spinResult.AdSpinAvailable));
        }

        private void RestoreOptimisticSpins(int previousSpinsAmount)
        {
            var normalizedPrevious = Mathf.Max(0, previousSpinsAmount);
            if (_currentSpinsAmount == normalizedPrevious)
            {
                return;
            }

            _currentSpinsAmount = normalizedPrevious;
            RefreshViewData();
        }

        private void SubscribeTimerService()
        {
            if (_fortuneWheelTimerService == null)
            {
                return;
            }

            _fortuneWheelTimerService.OnTimerUpdated -= HandleTimerUpdated;
            _fortuneWheelTimerService.OnStateUpdated -= HandleStateUpdated;
            _fortuneWheelTimerService.OnTimerUpdated += HandleTimerUpdated;
            _fortuneWheelTimerService.OnStateUpdated += HandleStateUpdated;
        }

        private void UnsubscribeTimerService()
        {
            if (_fortuneWheelTimerService == null)
            {
                return;
            }

            _fortuneWheelTimerService.OnTimerUpdated -= HandleTimerUpdated;
            _fortuneWheelTimerService.OnStateUpdated -= HandleStateUpdated;
        }

        private void StartTimerService(FortuneWheelDataServerItem initialData)
        {
            if (_fortuneWheelTimerService == null || initialData == null)
            {
                return;
            }

            try
            {
                _fortuneWheelTimerService.Start(initialData, _requestCts?.Token ?? CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(FortuneWheelController)}] Failed to start timer service: {exception.Message}");
            }
        }

        private void HandleTimerUpdated(TimeSpan remainingTime)
        {
            _currentRemainingTime = remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
            View.SetRemainingTime(_currentRemainingTime);
        }

        private void HandleStateUpdated(FortuneWheelDataServerItem state)
        {
            if (state == null)
            {
                return;
            }

            ApplyStateUpdate(state);
        }

        private void ApplyStateUpdate(FortuneWheelDataServerItem state)
        {
            _currentSpinsAmount = Mathf.Max(0, state.AvailableSpins);
            _isAdSpinAvailable = state.AdSpinAvailable;
            _currentRemainingTime = CalculateRemainingTime(state.NextUpdateAt);
            RefreshViewData();
            UpdateInteractionState();
        }

        private static int FindSectorIndexByRewardId(IReadOnlyList<FortuneWheelSectorArgs> sectors, string rewardId)
        {
            if (sectors == null || string.IsNullOrWhiteSpace(rewardId))
            {
                return -1;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                var sector = sectors[i];
                if (sector != null && string.Equals(sector.RewardId, rewardId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private void ShowRewardWindow(FortuneWheelSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return;
            }

            var rewardArgs = new RewardsWindowArgs(spinResult.RewardId);
            UIManager.Show<RewardsWindowController>(rewardArgs);
        }

        private static bool ValidateSectors(IReadOnlyList<FortuneWheelSectorArgs> sectors)
        {
            if (sectors == null || sectors.Count != FortuneWheelConfig.Gameplay.SectorCount)
            {
                var currentCount = sectors?.Count ?? 0;
                Debug.LogError($"[{nameof(FortuneWheelController)}] Wheel expects exactly {FortuneWheelConfig.Gameplay.SectorCount} sectors. Now is {currentCount}");
                return false;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                if (sectors[i] == null || string.IsNullOrWhiteSpace(sectors[i].RewardId))
                {
                    Debug.LogError($"[{nameof(FortuneWheelController)}] Sector {i} is null.");
                    return false;
                }
            }

            return true;
        }

        private void CloseWindow()
        {
            if (_isSpinning)
            {
                return;
            }

            UIManager.Hide<FortuneWheelController>();
        }

        private static long NormalizeUnixTimestampToSeconds(long unixTimestamp)
        {
            if (unixTimestamp <= 0)
            {
                return 0;
            }

            // Current Unix seconds are ~10 digits, while Unix milliseconds are ~13.
            return unixTimestamp >= 1_000_000_000_000L
                ? unixTimestamp / 1000L
                : unixTimestamp;
        }

        private static TimeSpan CalculateRemainingTime(long nextUpdateAt)
        {
            var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nextUpdateAtUnixSeconds = NormalizeUnixTimestampToSeconds(nextUpdateAt);
            var remainingSeconds = Math.Max(0L, nextUpdateAtUnixSeconds - nowUnixSeconds);
            return TimeSpan.FromSeconds(remainingSeconds);
        }
    }
}
