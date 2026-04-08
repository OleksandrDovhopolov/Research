using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardCollectionModule
    {
        string EventId { get; }

        UniTask<OpenPackResultDto> OpenPackAsync(string packId, CancellationToken ct = default);
        UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default);
        UniTask ResetNewFlagsAsync(IReadOnlyCollection<string> cardIds, CancellationToken ct = default);
        UniTask UnlockCards(IReadOnlyCollection<string> cardIds, CancellationToken ct = default);
        UniTask<EventCardsSaveData> Load(CancellationToken ct = default);
        UniTask PurgeEventDataAsync(CancellationToken ct = default);
    }

    public interface ICardCollectionPointsAccount
    {
        UniTask<bool> TryAddPointsAsync(int pointsToAdd, CancellationToken ct = default);
        UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default);
        UniTask<int> GetCollectionPoints(CancellationToken ct = default);
    }
}
