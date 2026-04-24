using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FortuneWheel
{
    public sealed class FortuneWheelTimerService : IFortuneWheelTimerService
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RefreshRetryInterval = TimeSpan.FromSeconds(5);

        private readonly IFortuneWheelServerService _fortuneWheelServerService;

        private CancellationTokenSource _sessionCts;
        private FortuneWheelDataServerItem _currentState;
        private bool _refreshInProgress;
        private long _lastRefreshFailureAtUnixSeconds = long.MinValue;

        public event Action<TimeSpan> OnTimerUpdated;
        public event Action<FortuneWheelDataServerItem> OnStateUpdated;

        public FortuneWheelTimerService(IFortuneWheelServerService fortuneWheelServerService)
        {
            _fortuneWheelServerService = fortuneWheelServerService ?? throw new ArgumentNullException(nameof(fortuneWheelServerService));
        }

        public void Start(FortuneWheelDataServerItem initialData, CancellationToken ct)
        {
            if (initialData == null)
            {
                throw new ArgumentNullException(nameof(initialData));
            }

            Stop();

            _currentState = NormalizeState(initialData);
            _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _lastRefreshFailureAtUnixSeconds = long.MinValue;
            _refreshInProgress = false;

            OnStateUpdated?.Invoke(_currentState);
            OnTimerUpdated?.Invoke(CalculateRemainingTime(_currentState.NextUpdateAt));

            RunHeartbeatAsync(_sessionCts.Token).Forget();
        }

        public void ApplySpinResult(FortuneWheelSpinResult spinResult)
        {
            if (spinResult == null)
            {
                return;
            }

            _currentState = NormalizeState(new FortuneWheelDataServerItem(
                spinResult.AvailableSpins,
                spinResult.UpdatedAt,
                spinResult.NextUpdateAt,
                spinResult.AdSpinAvailable));

            _lastRefreshFailureAtUnixSeconds = long.MinValue;
            OnStateUpdated?.Invoke(_currentState);
            OnTimerUpdated?.Invoke(CalculateRemainingTime(_currentState.NextUpdateAt));
        }

        public void Stop()
        {
            _refreshInProgress = false;
            _lastRefreshFailureAtUnixSeconds = long.MinValue;

            if (_sessionCts == null)
            {
                return;
            }

            _sessionCts.Cancel();
            _sessionCts.Dispose();
            _sessionCts = null;
        }

        private async UniTaskVoid RunHeartbeatAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await ProcessTickAsync(ct);
                    await UniTask.Delay(TickInterval, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask ProcessTickAsync(CancellationToken ct)
        {
            if (_currentState == null)
            {
                OnTimerUpdated?.Invoke(TimeSpan.Zero);
                return;
            }

            var remaining = CalculateRemainingTime(_currentState.NextUpdateAt);
            OnTimerUpdated?.Invoke(remaining);

            if (remaining > TimeSpan.Zero || _refreshInProgress)
            {
                return;
            }

            var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (_lastRefreshFailureAtUnixSeconds != long.MinValue &&
                nowUnixSeconds - _lastRefreshFailureAtUnixSeconds < (long)RefreshRetryInterval.TotalSeconds)
            {
                return;
            }

            _refreshInProgress = true;
            try
            {
                var freshData = await _fortuneWheelServerService.GetDataSync(ct);
                _currentState = NormalizeState(freshData);
                _lastRefreshFailureAtUnixSeconds = long.MinValue;

                OnStateUpdated?.Invoke(_currentState);
                OnTimerUpdated?.Invoke(CalculateRemainingTime(_currentState.NextUpdateAt));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _lastRefreshFailureAtUnixSeconds = nowUnixSeconds;
                OnTimerUpdated?.Invoke(TimeSpan.Zero);
                Debug.LogWarning($"[FortuneWheelTimerService] Failed to refresh FortuneWheel state at phase boundary: {exception.Message}");
            }
            finally
            {
                _refreshInProgress = false;
            }
        }

        private static FortuneWheelDataServerItem NormalizeState(FortuneWheelDataServerItem state)
        {
            var availableSpins = Math.Max(0, state.AvailableSpins);
            var updatedAt = Math.Max(0L, NormalizeUnixTimestampToSeconds(state.UpdatedAt));
            var nextUpdateAt = Math.Max(0L, NormalizeUnixTimestampToSeconds(state.NextUpdateAt));
            return new FortuneWheelDataServerItem(availableSpins, updatedAt, nextUpdateAt, state.AdSpinAvailable);
        }

        private static TimeSpan CalculateRemainingTime(long nextUpdateAt)
        {
            var nextUpdateAtUnixSeconds = NormalizeUnixTimestampToSeconds(nextUpdateAt);
            var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var remainingSeconds = Math.Max(0L, nextUpdateAtUnixSeconds - nowUnixSeconds);
            return TimeSpan.FromSeconds(remainingSeconds);
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
