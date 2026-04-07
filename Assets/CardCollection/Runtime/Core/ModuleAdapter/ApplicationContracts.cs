using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public readonly struct OpenPackResultDto
    {
        public static readonly OpenPackResultDto Empty = new(
            System.Array.Empty<string>(),
            System.Array.Empty<string>(),
            false,
            0,
            0);

        public IReadOnlyList<string> OpenedCardIds { get; }
        public IReadOnlyList<string> NewlyCompletedGroupIds { get; }
        public bool CollectionCompleted { get; }
        public int CurrentPoints { get; }
        public int AwardedDuplicatePoints { get; }

        public OpenPackResultDto(
            IReadOnlyList<string> openedCardIds,
            IReadOnlyList<string> newlyCompletedGroupIds,
            bool collectionCompleted,
            int currentPoints,
            int awardedDuplicatePoints)
        {
            OpenedCardIds = openedCardIds ?? System.Array.Empty<string>();
            NewlyCompletedGroupIds = newlyCompletedGroupIds ?? System.Array.Empty<string>();
            CollectionCompleted = collectionCompleted;
            CurrentPoints = currentPoints;
            AwardedDuplicatePoints = awardedDuplicatePoints;
        }
    }

    public readonly struct UnlockCardsResultDto
    {
        public static readonly UnlockCardsResultDto Empty = new(
            System.Array.Empty<string>(),
            System.Array.Empty<string>(),
            false,
            0);

        public IReadOnlyList<string> UnlockedCardIds { get; }
        public IReadOnlyList<string> NewlyCompletedGroupIds { get; }
        public bool CollectionCompleted { get; }
        public int AwardedDuplicatePoints { get; }

        public UnlockCardsResultDto(
            IReadOnlyList<string> unlockedCardIds,
            IReadOnlyList<string> newlyCompletedGroupIds,
            bool collectionCompleted,
            int awardedDuplicatePoints)
        {
            UnlockedCardIds = unlockedCardIds ?? System.Array.Empty<string>();
            NewlyCompletedGroupIds = newlyCompletedGroupIds ?? System.Array.Empty<string>();
            CollectionCompleted = collectionCompleted;
            AwardedDuplicatePoints = awardedDuplicatePoints;
        }
    }

    public interface IOpenPackUseCase
    {
        UniTask<OpenPackResultDto> ExecuteAsync(string eventId, string packId, CancellationToken ct = default);
    }

    public interface IUnlockCardsUseCase
    {
        UniTask<UnlockCardsResultDto> ExecuteAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default);
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
    }

    public interface ICardCollectionApplicationFacade :
        ICardCollectionModule,
        ICardCollectionPointsAccount,
        ICardGroupCompletionNotifier,
        ICardCollectionCompletionNotifier,
        System.IDisposable
    {
    }
}
