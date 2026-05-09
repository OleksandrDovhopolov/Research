using System.Threading;
using Cysharp.Threading.Tasks;

namespace Infrastructure
{
    public interface ISaveDebugMirror
    {
        UniTask WriteAsync(string json, CancellationToken cancellationToken);
        UniTask DeleteAsync(CancellationToken cancellationToken);
    }
}
