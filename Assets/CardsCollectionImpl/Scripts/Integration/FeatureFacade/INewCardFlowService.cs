using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface INewCardFlowService
    {
        UniTask<NewCardScreenData> LoadAsync(string packId, CancellationToken ct);
    }
}
