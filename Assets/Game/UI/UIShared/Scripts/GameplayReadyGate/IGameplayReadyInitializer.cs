using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIShared
{
    public interface IGameplayReadyInitializer
    {
        UniTask InitializeAsync(CancellationToken ct);
    }
}
