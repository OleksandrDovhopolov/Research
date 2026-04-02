using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public interface ICardCollectionModule
    {
        string EventId { get; }

        CardPack GetPackById(string packId);
        UniTask<List<string>> OpenPackAndUnlockAsync(string packId, CancellationToken ct = default);
        UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default);
        UniTask ResetNewFlagsAsync(IReadOnlyCollection<string> cardIds, CancellationToken ct = default);
    }

    public interface ICardCollectionPointsAccount
    {
        UniTask<bool> TryAddPointsAsync(int pointsToAdd, CancellationToken ct = default);
        UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default);
    }
    
    public interface ICardCollectionReader
    {
        UniTask<EventCardsSaveData> Load(CancellationToken ct = default);
        UniTask<HashSet<string>> GetMissingCardIdsAsync(List<CardDefinition> allCards, CancellationToken ct = default);
        UniTask<int> GetCollectionPoints(CancellationToken ct = default);
    }

    public interface ICardCollectionUpdater
    {
        UniTask UnlockCard(string cardId, CancellationToken ct = default);
        UniTask UnlockCards(IReadOnlyCollection<string> cardIds, CancellationToken ct = default);
        UniTask Save(CancellationToken ct = default);
        UniTask Clear(CancellationToken ct = default);
    }
}
