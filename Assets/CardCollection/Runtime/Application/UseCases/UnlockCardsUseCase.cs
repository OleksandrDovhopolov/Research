using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    public sealed class UnlockCardsUseCase : IUnlockCardsUseCase
    {
        private readonly CardProgressService _cardProgressService;
        private readonly ICardDefinitionProvider _cardDefinitionProvider;

        public UnlockCardsUseCase(CardProgressService cardProgressService, ICardDefinitionProvider cardDefinitionProvider)
        {
            _cardProgressService = cardProgressService ?? throw new ArgumentNullException(nameof(cardProgressService));
            _cardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
        }

        public async UniTask<UnlockCardsResultDto> ExecuteAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (cardIds == null || cardIds.Count == 0)
            {
                return UnlockCardsResultDto.Empty;
            }

            var data = await _cardProgressService.LoadAsync(eventId, ct);
            var unlockedBefore = new HashSet<string>(
                data.Cards.Where(card => card is { IsUnlocked: true }).Select(card => card.CardId),
                StringComparer.Ordinal);

            await _cardProgressService.UnlockCardsAsync(eventId, cardIds, ct);

            var afterData = await _cardProgressService.LoadAsync(eventId, ct);
            var unlockedAfter = new HashSet<string>(
                afterData.Cards.Where(card => card is { IsUnlocked: true }).Select(card => card.CardId),
                StringComparer.Ordinal);

            var unlockedCardIds = cardIds
                .Where(id => !string.IsNullOrEmpty(id) && !unlockedBefore.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var completion = CompletionOutcomeEvaluator.Evaluate(
                _cardDefinitionProvider.GetCardDefinitions(),
                unlockedBefore,
                unlockedAfter);

            return unlockedCardIds.Length == 0 && completion.NewlyCompletedGroupIds.Count == 0 && !completion.CollectionCompleted
                ? UnlockCardsResultDto.Empty
                : new UnlockCardsResultDto(unlockedCardIds, completion.NewlyCompletedGroupIds, completion.CollectionCompleted, 0);
        }
    }
}
