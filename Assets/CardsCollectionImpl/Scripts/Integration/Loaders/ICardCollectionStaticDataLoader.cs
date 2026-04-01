using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace CardCollectionImpl
{
    public interface ICardCollectionStaticDataLoader
    {
        UniTask<CardCollectionStaticData> LoadAsync(CardCollectionEventModel model, CancellationToken ct);
    }
}
