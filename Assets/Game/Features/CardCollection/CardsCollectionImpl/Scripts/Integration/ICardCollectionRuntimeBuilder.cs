using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface ICardCollectionRuntimeBuilder
    {
        UniTask<CardCollectionSession> BuildAsync(CardCollectionEventModel model, CancellationToken ct);
    }
}