using System.Threading;
using Cysharp.Threading.Tasks;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public interface ICardCollectionApplicationFacadeFactory
    {
        UniTask<ICardCollectionApplicationFacade> CreateInitializedAsync(
            CardCollectionStaticData staticData,
            string eventId,
            CancellationToken ct);
    }
}
