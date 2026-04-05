using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIShared
{
    public sealed class GameplayReadyGate : IGameplayReadyGate
    {
        private readonly UniTaskCompletionSource _readyTcs = new();
        private bool _isReady;

        public bool IsReady => _isReady;

        public UniTask WaitUntilReadyAsync(CancellationToken ct)
        {
            if (_isReady)
                return UniTask.CompletedTask;

            ct.ThrowIfCancellationRequested();
            return _readyTcs.Task.AttachExternalCancellation(ct);
        }

        public void MarkReady()
        {
            if (_isReady)
                return;

            _isReady = true;
            _readyTcs.TrySetResult();
        }
    }
}