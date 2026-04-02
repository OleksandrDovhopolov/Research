using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace CardCollectionImpl
{
    public interface ICardCollectionSessionFactory
    {
        UniTask<CardCollectionSession> CreateAsync(
            CardCollectionEventModel model,
            CardCollectionStaticData staticData,
            ICardCollectionApplicationFacade facade,
            CancellationToken ct);
    }
}
