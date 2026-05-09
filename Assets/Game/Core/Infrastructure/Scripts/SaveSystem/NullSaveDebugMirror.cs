using System.Threading;
using Cysharp.Threading.Tasks;

namespace Infrastructure
{
    public sealed class NullSaveDebugMirror : ISaveDebugMirror
    {
        public UniTask WriteAsync(string json, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        public UniTask DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }
    }
}
