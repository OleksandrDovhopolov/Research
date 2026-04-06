using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIShared
{
    public interface IGameplayReadyGate
    {
        bool IsReady { get; }
        UniTask WaitUntilReadyAsync(CancellationToken ct);
        UniTask MarkReadyAsync(CancellationToken ct);
    }
}