using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface IOpenPackFlowService
    {
        UniTask<NewCardScreenData> LoadAsync(string packId, CancellationToken ct);
    }
}
