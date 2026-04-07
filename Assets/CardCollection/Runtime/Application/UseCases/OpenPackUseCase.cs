using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public sealed class OpenPackUseCase : IOpenPackUseCase
    {
        private readonly CardPackService _cardPackService;
        private readonly PackBasedCardsRandomizer _cardRandomizer;
        private readonly CardProgressService _cardProgressService;
        private readonly IDuplicateCardPointsCalculator _duplicateCardPointsCalculator;
        private readonly ICardDefinitionProvider _cardDefinitionProvider;

        public OpenPackUseCase(
            CardPackService cardPackService,
            PackBasedCardsRandomizer cardRandomizer,
            CardProgressService cardProgressService,
            IDuplicateCardPointsCalculator duplicateCardPointsCalculator,
            ICardDefinitionProvider cardDefinitionProvider)
        {
            _cardPackService = cardPackService ?? throw new ArgumentNullException(nameof(cardPackService));
            _cardRandomizer = cardRandomizer ?? throw new ArgumentNullException(nameof(cardRandomizer));
            _cardProgressService = cardProgressService ?? throw new ArgumentNullException(nameof(cardProgressService));
            _duplicateCardPointsCalculator = duplicateCardPointsCalculator ?? throw new ArgumentNullException(nameof(duplicateCardPointsCalculator));
            _cardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
        }

        public async UniTask<OpenPackResultDto> ExecuteAsync(string eventId, string packId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var pack = _cardPackService.GetPackById(packId);
            if (pack == null)
            {
                return OpenPackResultDto.Empty;
            }

            var beforeData = await _cardProgressService.LoadAsync(eventId, ct);
            var unlockedBefore = new HashSet<string>(
                beforeData.Cards.Where(card => card is { IsUnlocked: true } && !string.IsNullOrEmpty(card.CardId))
                    .Select(card => card.CardId),
                StringComparer.Ordinal);

            var openedCardIds = await _cardRandomizer.GetRandomNewCardsAsync(pack, ct);
            if (openedCardIds == null || openedCardIds.Count == 0)
            {
                return OpenPackResultDto.Empty;
            }

            var openedCardsProgress = await _cardProgressService.GetCardsByIdsAsync(eventId, openedCardIds, ct);
            var duplicatePoints = _duplicateCardPointsCalculator.Calculate(openedCardIds, openedCardsProgress);
            if (duplicatePoints.HasPoints)
            {
                await _cardProgressService.AddPointsAsync(eventId, duplicatePoints.TotalPoints, ct);
            }

            await _cardProgressService.UnlockCardsAsync(eventId, openedCardIds, ct);

            var afterData = await _cardProgressService.LoadAsync(eventId, ct);
            var unlockedAfter = new HashSet<string>(
                afterData.Cards.Where(card => card is { IsUnlocked: true } && !string.IsNullOrEmpty(card.CardId))
                    .Select(card => card.CardId),
                StringComparer.Ordinal);

            var completion = CompletionOutcomeEvaluator.Evaluate(
                _cardDefinitionProvider.GetCardDefinitions(),
                unlockedBefore,
                unlockedAfter);

            return new OpenPackResultDto(
                openedCardIds,
                completion.NewlyCompletedGroupIds,
                completion.CollectionCompleted,
                duplicatePoints.TotalPoints);
        }
    }
}
