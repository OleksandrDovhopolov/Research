using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace CardCollectionImpl
{
    public interface ICardCollectionRuntimeBuilder
    {
        UniTask<CardCollectionSession> BuildAsync(CardCollectionEventModel model, CancellationToken ct);
    }
}