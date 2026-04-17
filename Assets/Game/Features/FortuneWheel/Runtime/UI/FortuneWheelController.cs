using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        public const int SectorCount = 8;

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

        private FortuneWheelArgs Args => Arguments as FortuneWheelArgs;
        private IReadOnlyList<FortuneWheelSectorArgs> Sectors => Args?.Sectors ?? Array.Empty<FortuneWheelSectorArgs>();

        private int _currentSpinsAmount;
        private bool _isSpinning;
        private bool _isSpinRequestInProgress;
        private bool _isDataValid;
        private CancellationTokenSource _requestCts;
        private TimeSpan _currentRemainingTime;

        public override bool IsCloseBlocked => _isSpinning;

        [Inject]
        private void Construct(
            IFortuneWheelServerService fortuneWheelServerService,
            IFortuneWheelTimerService fortuneWheelTimerService)
        {
            _fortuneWheelServerService = fortuneWheelServerService;
            _fortuneWheelTimerService = fortuneWheelTimerService;
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
                _currentRemainingTime = TimeSpan.Zero;
                View.SetData(args);
                RefreshViewData();
                UpdateInteractionState();
                return;
            }

            _currentSpinsAmount = Mathf.Max(0, args.InitialData.AvailableSpins);
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
        }

        protected override void OnHideStart(bool isClosed)
        {
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

        private async UniTaskVoid OnSpinClickedAsync(CancellationToken ct)
        {
            if (_isSpinning || _isSpinRequestInProgress || !_isDataValid || _currentSpinsAmount <= 0)
            {
                return;
            }

            if (_fortuneWheelServerService == null)
            {
                Debug.LogError($"[{nameof(FortuneWheelController)}] {nameof(IFortuneWheelServerService)} is not available.");
                return;
            }

            var previousSpinsAmount = _currentSpinsAmount;
            _currentSpinsAmount = Mathf.Max(0, _currentSpinsAmount - 1);
            _isSpinRequestInProgress = true;
            RefreshViewData();
            UpdateInteractionState();

            try
            {
                var spinResult = await _fortuneWheelServerService.SpinAsync(ct);
                Debug.LogWarning($"[{GetType().Name}] spinResult with RewardId {spinResult.RewardId}");
                ApplySpinResult(spinResult);
                var targetSectorIndex = FindSectorIndexByRewardId(Sectors, spinResult.RewardId);

                if (targetSectorIndex < 0)
                {
                    Debug.LogWarning($"[{nameof(FortuneWheelController)}] Reward id '{spinResult.RewardId}' was not found in sectors.");
                    return;
                }

                _isSpinning = true;
                UpdateInteractionState();

                var animationStarted = View.PlaySpinToSector(targetSectorIndex, OnSpinCompleted);
                if (!animationStarted)
                {
                    _isSpinning = false;
                    UpdateInteractionState();
                }
            }
            catch (OperationCanceledException)
            {
                RestoreOptimisticSpins(previousSpinsAmount);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{nameof(FortuneWheelController)}] Spin request failed. {exception.Message}");
                RestoreOptimisticSpins(previousSpinsAmount);
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
            if (!_isSpinning)
            {
                return;
            }

            _isSpinning = false;
            UpdateInteractionState();
            
            //TODO play animation
            //_animationService.Animate(animationStartPosition, rewardViewData.Amount, rewardViewData.Id, rewardViewData.Icon);
        }

        private void UpdateInteractionState()
        {
            View.SetSpinInteractable(_isDataValid && !_isSpinning && !_isSpinRequestInProgress && _currentSpinsAmount > 0);
            View.SetCloseInteractable(_isSpinning);
        }

        private void CancelSpin()
        {
            _isSpinning = false;
            _isSpinRequestInProgress = false;
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
                spinResult.NextUpdateAt));
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

        private static bool ValidateSectors(IReadOnlyList<FortuneWheelSectorArgs> sectors)
        {
            if (sectors == null || sectors.Count != FortuneWheelArgs.SectorCount)
            {
                var currentCount = sectors?.Count ?? 0;
                Debug.LogError($"[{nameof(FortuneWheelController)}] Wheel expects exactly {FortuneWheelArgs.SectorCount} sectors. Now is {currentCount}");
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
