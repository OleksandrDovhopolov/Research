using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface ICardCollectionSessionFacade
    {
        internal void SetActiveSession(CardCollectionSessionContext sessionContext);
        UniTask TryOpenPackById(string packId, CancellationToken ct);
        UniTask TryUnlockCards(IReadOnlyCollection<string> cardIds, CancellationToken ct);
        UniTask TryAddPoints(int points, CancellationToken ct);
        UniTask TryRemovePoints(int points, CancellationToken ct);
        UniTask TryCompleteAllCollection(CancellationToken ct);
        UniTask TryUnlockAllMinusOneCard(CancellationToken ct);
        UniTask TryUnlockFirstNineCardsInEachGroup(CancellationToken ct);
        UniTask TryUnlockGroupByIndex(int groupIndex, CancellationToken ct);
        internal void ClearSession();
    }
}