using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface ICardCollectionFeatureFacade
    {
        bool IsActive { get; }
        CardCollectionSessionContext FeatureContext { get; }

        void SetActiveSession(CardCollectionSessionContext sessionContext);
        void ClearSession();
        bool TryGetCollectionUpdater(out ICardCollectionUpdater updater);
        bool TryGetCollectionReader(out ICardCollectionReader reader);
        bool TryGetCollectionPointsAccount(out ICardCollectionPointsAccount pointsAccount);
        UniTask ShowCardCollectionWindow(CancellationToken ct);
        UniTask ShowNewCardWindow(string packId, CancellationToken ct);
    }
}