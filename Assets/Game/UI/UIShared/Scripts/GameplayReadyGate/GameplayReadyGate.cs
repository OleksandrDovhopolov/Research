using System;
using System.Collections.Generic;
using System.Threading;
using Analytics;
using Cysharp.Threading.Tasks;

namespace UIShared
{
    public sealed class GameplayReadyGate : IGameplayReadyGate
    {
        private readonly IAnalyticsService _analytics;
        private readonly IAnalyticsEventFactory _events;
        
        private readonly TransitionAnimationService _transitionAnimationService;
        private readonly IReadOnlyList<IGameplayReadyInitializer> _initializers;
        private readonly UniTaskCompletionSource _readyTcs = new();

        private bool _isReady;
        private bool _isMarkingReady;

        public bool IsReady => _isReady;

        public GameplayReadyGate(
            IAnalyticsService  analytics,
            IAnalyticsEventFactory events,
            TransitionAnimationService transitionAnimationService,
            IEnumerable<IGameplayReadyInitializer> initializers)
        {
            _analytics = analytics;
            _events = events;
            _transitionAnimationService = transitionAnimationService
                                          ?? throw new ArgumentNullException(nameof(transitionAnimationService));
            _initializers = initializers != null
                ? new List<IGameplayReadyInitializer>(initializers)
                : Array.Empty<IGameplayReadyInitializer>();
        }

        public UniTask WaitUntilReadyAsync(CancellationToken ct)
        {
            if (_isReady)
                return UniTask.CompletedTask;

            ct.ThrowIfCancellationRequested();
            return _readyTcs.Task.AttachExternalCancellation(ct);
        }

        public async UniTask MarkReadyAsync(CancellationToken ct)
        {
            if (_isReady)
                return;

            if (_isMarkingReady)
            {
                await WaitUntilReadyAsync(ct);
                return;
            }

            _isMarkingReady = true;
            try
            {
                ct.ThrowIfCancellationRequested();
                await RunInitializersAsync(ct);
                await _transitionAnimationService.PlayRevealAsync(ct);

                _analytics.TrackEvent(_events.CreateEvent(AnalyticsEventNames.AppStarted));
                
                _isReady = true;
                _readyTcs.TrySetResult();
            }
            finally
            {
                _isMarkingReady = false;
            }
        }

        private async UniTask RunInitializersAsync(CancellationToken ct)
        {
            for (var i = 0; i < _initializers.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await _initializers[i].InitializeAsync(ct);
            }
        }
    }
}
