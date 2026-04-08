using System.Threading;
using Cysharp.Threading.Tasks;

namespace Infrastructure
{
    public interface ISaveStorage
    {
        UniTask SaveAsync(string data, CancellationToken cancellationToken);
        UniTask<string> LoadAsync(CancellationToken cancellationToken);
        bool Exists();
        UniTask DeleteAsync(CancellationToken cancellationToken);
        UniTask<long> GetLastModifiedTimestampAsync(CancellationToken cancellationToken);
    }
}
