using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIShared
{
    public sealed class GameplayReadyGate : IGameplayReadyGate
    {
        private readonly TransitionAnimationService _transitionAnimationService;
        private readonly UniTaskCompletionSource _readyTcs = new();

        private bool _isReady;
        private bool _isMarkingReady;

        public bool IsReady => _isReady;

        public GameplayReadyGate(TransitionAnimationService transitionAnimationService)
        {
            _transitionAnimationService = transitionAnimationService
                                          ?? throw new ArgumentNullException(nameof(transitionAnimationService));
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
                await _transitionAnimationService.PlayRevealAsync(ct);

                _isReady = true;
                _readyTcs.TrySetResult();
            }
            finally
            {
                _isMarkingReady = false;
            }
        }
    }
}