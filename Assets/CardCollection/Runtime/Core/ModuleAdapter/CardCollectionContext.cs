using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    /// <summary>
    /// Internal context that constructs and owns all core services.
    /// Exposes delegation methods so callers never reach through to internal services (Law of Demeter).
    /// </summary>
    internal sealed class CardCollectionContext : IDisposable
    {
        private readonly CardCollectionModuleConfig _config;

        private readonly CardPackService _cardPackService;
        private readonly PackBasedCardsRandomizer _cardRandomizer;
        private readonly CardProgressService _cardProgressService;
        private readonly IDuplicateCardPointsCalculator _duplicateCardPointsCalculator;

        public string DefaultEventId { get; }

        public CardCollectionContext(CardCollectionModuleConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.PackProvider == null) throw new ArgumentNullException(nameof(config.PackProvider));
            if (config.CardSelector == null) throw new ArgumentNullException(nameof(config.CardSelector));
            if (config.EventCardsStorage == null) throw new ArgumentNullException(nameof(config.EventCardsStorage));

            _config = config;

            DefaultEventId = _config.DefaultEventId;
            _cardPackService = new CardPackService(_config.PackProvider);
            _cardProgressService = new CardProgressService(_config.EventCardsStorage);
            _cardRandomizer = new PackBasedCardsRandomizer(_config.CardSelector, _config.CardDefinitionProvider);
            _duplicateCardPointsCalculator = new DuplicateCardPointsCalculator(_config.CardDefinitionProvider);
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await _cardPackService.InitializeAsync(ct);
            await _cardProgressService.InitializeAsync(ct);
        }

        // ── CardPackService delegation ──

        public List<CardPack> GetAllPacks() => _cardPackService.GetAllPacks();

        public CardPack GetPackById(string packId) => _cardPackService.GetPackById(packId);

        // ── PackBasedCardsRandomizer delegation ──

        public UniTask<List<string>> GetRandomNewCardsAsync(CardPack pack, CardSelectionContext context, CancellationToken ct = default)
            => _cardRandomizer.GetRandomNewCardsAsync(pack, context, ct);

        // ── CardProgressService delegation ──

        public UniTask<EventCardsSaveData> LoadAsync(string eventId, CancellationToken ct = default)
            => _cardProgressService.LoadAsync(eventId, ct);

        public UniTask SaveAsync(EventCardsSaveData data, CancellationToken ct = default)
            => _cardProgressService.SaveAsync(data, ct);

        public UniTask UnlockCardAsync(string eventId, string cardId, CancellationToken ct = default)
            => _cardProgressService.UnlockCardAsync(eventId, cardId, ct);

        public UniTask UnlockCardsAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
            => _cardProgressService.UnlockCardsAsync(eventId, cardIds, ct);

        public UniTask<List<CardProgressData>> GetCardsByIdsAsync(string eventId, List<string> cardIds, CancellationToken ct = default)
            => _cardProgressService.GetCardsByIdsAsync(eventId, cardIds, ct);

        public UniTask ResetNewFlagAsync(string eventId, string cardId, CancellationToken ct = default)
            => _cardProgressService.ResetNewFlagAsync(eventId, cardId, ct);

        public int GetPoints(string eventId)
            => _cardProgressService.GetPoints(eventId);

        public UniTask AddPointsAsync(string eventId, int pointsToAdd, CancellationToken ct = default)
            => _cardProgressService.AddPointsAsync(eventId, pointsToAdd, ct);

        public UniTask<HashSet<string>> GetMissingCardIdsAsync(string eventId, List<CardDefinition> allCards, CancellationToken ct = default)
            => _cardProgressService.GetMissingCardIdsAsync(eventId, allCards, ct);

        public UniTask ClearCollectionAsync(CancellationToken ct = default)
            => _cardProgressService.ClearCollectionAsync(ct);

        // ── DuplicateCardPointsCalculator delegation ──

        public DuplicateCardPointsCalculation CalculateDuplicatePoints(
            IReadOnlyList<string> openedCardIds,
            IReadOnlyCollection<CardProgressData> openedCardsProgress)
            => _duplicateCardPointsCalculator.Calculate(openedCardIds, openedCardsProgress);

        // ── ICardDefinitionProvider delegation ──

        public List<CardDefinition> GetCardDefinitions() => _config.CardDefinitionProvider.GetCardDefinitions();

        public void Dispose()
        {
            _cardPackService.Dispose();
            _cardProgressService.Dispose();
        }
    }
}
