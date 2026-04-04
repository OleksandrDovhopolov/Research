using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public sealed class CardCollectionApplicationFacade : ICardCollectionApplicationFacade
    {
        private readonly string _eventId;
        private readonly ICardDefinitionProvider _cardDefinitionProvider;
        private readonly CardPackService _cardPackService;
        private readonly CardProgressService _cardProgressService;
        private readonly IOpenPackUseCase _openPackUseCase;
        private readonly IUnlockCardsUseCase _unlockCardsUseCase;
        private readonly IPointsAccountService _pointsAccountService;
        private readonly ICollectionProgressQueryService _progressQueryService;

        private bool _isCollectionCompleted;

        public event Action<CardGroupsCompletedData> OnGroupCompleted;
        public event Action<CardCollectionCompletedData> OnCollectionCompleted;

        public string EventId => _eventId;

        public CardCollectionApplicationFacade(
            string eventId,
            ICardDefinitionProvider cardDefinitionProvider,
            CardPackService cardPackService,
            CardProgressService cardProgressService,
            IOpenPackUseCase openPackUseCase,
            IUnlockCardsUseCase unlockCardsUseCase,
            IPointsAccountService pointsAccountService,
            ICollectionProgressQueryService progressQueryService)
        {
            if (string.IsNullOrEmpty(eventId)) throw new ArgumentException("Event id cannot be null or empty", nameof(eventId));
            _eventId = eventId;
            _cardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
            _cardPackService = cardPackService ?? throw new ArgumentNullException(nameof(cardPackService));
            _cardProgressService = cardProgressService ?? throw new ArgumentNullException(nameof(cardProgressService));
            _openPackUseCase = openPackUseCase ?? throw new ArgumentNullException(nameof(openPackUseCase));
            _unlockCardsUseCase = unlockCardsUseCase ?? throw new ArgumentNullException(nameof(unlockCardsUseCase));
            _pointsAccountService = pointsAccountService ?? throw new ArgumentNullException(nameof(pointsAccountService));
            _progressQueryService = progressQueryService ?? throw new ArgumentNullException(nameof(progressQueryService));
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await _cardPackService.InitializeAsync(ct);
            await _cardProgressService.InitializeAsync(ct);

            var data = await EnsureEventDataInitializedAsync(ct);
            _isCollectionCompleted = CompletionOutcomeEvaluator
                .Evaluate(_cardDefinitionProvider.GetCardDefinitions(), Array.Empty<string>(),
                    data.Cards.Where(card => card is { IsUnlocked: true } && !string.IsNullOrEmpty(card.CardId))
                        .Select(card => card.CardId)
                        .ToArray())
                .CollectionCompleted;
        }

        public CardPack GetPackById(string packId) => _cardPackService.GetPackById(packId);

        public async UniTask<List<string>> OpenPackAndUnlockAsync(string packId, CancellationToken ct = default)
        {
            var result = await _openPackUseCase.ExecuteAsync(_eventId, packId, ct);
            PublishCompletion(result.NewlyCompletedGroupIds, result.CollectionCompleted);
            return result.UnlockedCardIds.ToList();
        }

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(List<string> cardIds, CancellationToken ct = default)
        {
            return _progressQueryService.GetCardsByIdsAsync(_eventId, cardIds, ct);
        }

        public UniTask ResetNewFlagsAsync(IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            return _cardProgressService.ResetNewFlagsAsync(_eventId, cardIds, ct);
        }

        public UniTask<bool> TryAddPointsAsync(int pointsToAdd, CancellationToken ct = default)
        {
            return _pointsAccountService.TryAddAsync(_eventId, pointsToAdd, ct);
        }

        public UniTask<bool> TrySpendPointsAsync(int pointsToSpend, CancellationToken ct = default)
        {
            return _pointsAccountService.TrySpendAsync(_eventId, pointsToSpend, ct);
        }

        public async UniTask UnlockCards(IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            var result = await _unlockCardsUseCase.ExecuteAsync(_eventId, cardIds, ct);
            PublishCompletion(result.NewlyCompletedGroupIds, result.CollectionCompleted);
        }

        public UniTask<EventCardsSaveData> Load(CancellationToken ct = default)
        {
            return _progressQueryService.LoadAsync(_eventId, ct);
        }

        public UniTask<int> GetCollectionPoints(CancellationToken ct = default)
        {
            return _pointsAccountService.GetBalanceAsync(_eventId, ct);
        }

        public void Dispose()
        {
            _cardPackService.Dispose();
            _cardProgressService.Dispose();
        }

        private async UniTask<EventCardsSaveData> EnsureEventDataInitializedAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var data = await _progressQueryService.LoadAsync(_eventId, ct);
            if (data?.Cards != null && data.Cards.Count > 0)
            {
                return data;
            }

            var initializedData = new EventCardsSaveData
            {
                EventId = _eventId,
                Points = data?.Points ?? 0,
                Version = data?.Version ?? 1
            };

            foreach (var definition in _cardDefinitionProvider.GetCardDefinitions())
            {
                initializedData.Cards.Add(new CardProgressData
                {
                    CardId = definition.Id,
                    IsUnlocked = false
                });
            }

            await _cardProgressService.SaveAsync(initializedData, ct);
            return initializedData;
        }

        private void PublishCompletion(IReadOnlyList<string> newlyCompletedGroupIds, bool collectionCompleted)
        {
            if (newlyCompletedGroupIds is { Count: > 0 })
            {
                var groups = newlyCompletedGroupIds
                    .Select(groupId => new CardGroupCompletedData { GroupType = groupId })
                    .ToList();
                OnGroupCompleted?.Invoke(new CardGroupsCompletedData(groups));
            }

            if (collectionCompleted && !_isCollectionCompleted)
            {
                _isCollectionCompleted = true;
                OnCollectionCompleted?.Invoke(new CardCollectionCompletedData { EventId = _eventId });
            }
        }
    }
}
