using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface ICardCollectionFeatureFacade
    {
        bool IsActive { get; }

        void SetActiveSession(CardCollectionSessionContext sessionContext);
        bool TryGetCollectionModule(out ICardCollectionModule module);
        bool TryGetCollectionPointsAccount(out ICardCollectionPointsAccount pointsAccount);
        UniTask ShowNewCardWindow(string packId, CancellationToken ct);
        void ClearSession();
    }
}