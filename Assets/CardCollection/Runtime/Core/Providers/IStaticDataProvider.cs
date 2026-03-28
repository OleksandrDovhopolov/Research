using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface IStaticDataProvider<T>
    {
        T Data { get; }
        UniTask<T> LoadAsync(string fileName, CancellationToken ct = default); 
        void ClearCache();
    }
}