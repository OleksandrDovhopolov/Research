using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface IOpenPackFlow
    {
        UniTask OpenPackById(string packId, CancellationToken ct);
        UniTask ShowPendingGroupCompletedAsync(CancellationToken ct);
    }
}