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

        public int SpinsAmount { get; }
        public TimeSpan RemainingTime { get; }
        public IReadOnlyList<FortuneWheelSectorArgs> Sectors { get; }

        public FortuneWheelArgs(int spinsAmount, TimeSpan remainingTime, IReadOnlyList<FortuneWheelSectorArgs> sectors)
        {
            SpinsAmount = spinsAmount;
            RemainingTime = remainingTime;
            Sectors = sectors;
        }
    }

    [Window("FortuneWheelWindow")]
    public class FortuneWheelController : WindowController<FortuneWheelView>
    {
        private IFortuneWheelServerService _fortuneWheelServerService;

        private FortuneWheelArgs Args => Arguments as FortuneWheelArgs;
        private IReadOnlyList<FortuneWheelSectorArgs> Sectors => Args?.Sectors ?? Array.Empty<FortuneWheelSectorArgs>();

        private int _currentSpinsAmount;
        private bool _isSpinning;
        private bool _isSpinRequestInProgress;
        private bool _isDataValid;
        private CancellationTokenSource _requestCts;
        private FortuneWheelSpinResult _pendingSpinResult;
        private TimeSpan _currentRemainingTime;

        public override bool IsCloseBlocked => _isSpinning;

        [Inject]
        private void Construct(IFortuneWheelServerService fortuneWheelServerService)
        {
            _fortuneWheelServerService = fortuneWheelServerService;
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
                UpdateInteractionState();
                return;
            }

            _currentSpinsAmount = Mathf.Max(0, args.SpinsAmount);
            _currentRemainingTime = args.RemainingTime;
            _isDataValid = ValidateSectors(Sectors);

            RefreshViewData();
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
                var targetSectorIndex = FindSectorIndexByRewardId(Sectors, spinResult.RewardId);

                if (targetSectorIndex < 0)
                {
                    Debug.LogWarning($"[{nameof(FortuneWheelController)}] Reward id '{spinResult.RewardId}' was not found in sectors.");
                    RestoreOptimisticSpins(previousSpinsAmount);
                    return;
                }

                _pendingSpinResult = spinResult;
                _isSpinning = true;
                UpdateInteractionState();

                var animationStarted = View.PlaySpinToSector(targetSectorIndex, OnSpinCompleted);
                if (!animationStarted)
                {
                    _isSpinning = false;
                    ApplySpinResult(_pendingSpinResult);
                    _pendingSpinResult = null;
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

            if (_pendingSpinResult != null)
            {
                ApplySpinResult(_pendingSpinResult);
                _pendingSpinResult = null;
            }

            UpdateInteractionState();
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
            _pendingSpinResult = null;
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
            View.SetData(new FortuneWheelArgs(_currentSpinsAmount, _currentRemainingTime, Sectors));
        }

        private void ApplySpinResult(FortuneWheelSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return;
            }

            _currentSpinsAmount = Mathf.Max(0, spinResult.AvailableSpins);
            var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nextUpdateAtUnixSeconds = NormalizeUnixTimestampToSeconds(spinResult.NextUpdateAt);
            var remainingSeconds = Math.Max(0L, nextUpdateAtUnixSeconds - nowUnixSeconds);
            _currentRemainingTime = TimeSpan.FromSeconds(remainingSeconds);
            RefreshViewData();
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
    }
}
