using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface IOpenPackFlow
    {
        UniTask TryOpenPackById(string packId, CancellationToken ct);
    }
}