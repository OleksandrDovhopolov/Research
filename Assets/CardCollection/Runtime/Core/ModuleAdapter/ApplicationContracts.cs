using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public readonly struct OpenPackResult
    {
        public static readonly OpenPackResult Empty = new(System.Array.Empty<string>(), 0);

        public IReadOnlyList<string> OpenedCardIds { get; }
        public int DuplicatePointsAwarded { get; }

        public OpenPackResult(IReadOnlyList<string> openedCardIds, int duplicatePointsAwarded)
        {
            OpenedCardIds = openedCardIds ?? System.Array.Empty<string>();
            DuplicatePointsAwarded = duplicatePointsAwarded;
        }
    }

    public readonly struct UnlockCardsResult
    {
        public static readonly UnlockCardsResult Empty = new(System.Array.Empty<string>());

        public IReadOnlyList<string> NewlyUnlockedCardIds { get; }

        public UnlockCardsResult(IReadOnlyList<string> newlyUnlockedCardIds)
        {
            NewlyUnlockedCardIds = newlyUnlockedCardIds ?? System.Array.Empty<string>();
        }
    }

    public interface IOpenPackUseCase
    {
        UniTask<OpenPackResult> ExecuteAsync(string eventId, string packId, CancellationToken ct = default);
    }

    public interface IUnlockCardsUseCase
    {
        UniTask<UnlockCardsResult> ExecuteAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default);
    }

    public interface IPointsAccountService
    {
        UniTask<bool> TryAddAsync(string eventId, int pointsToAdd, CancellationToken ct = default);
        UniTask<bool> TrySpendAsync(string eventId, int pointsToSpend, CancellationToken ct = default);
        UniTask<int> GetBalanceAsync(string eventId, CancellationToken ct = default);
    }

    public interface ICollectionProgressQueryService
    {
        UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default);
        UniTask<List<CardProgressData>> GetCardsByIdsAsync(string eventId, List<string> cardIds, CancellationToken ct = default);
        UniTask<HashSet<string>> GetMissingCardIdsAsync(string eventId, List<CardDefinition> allCards, CancellationToken ct = default);
    }
}
