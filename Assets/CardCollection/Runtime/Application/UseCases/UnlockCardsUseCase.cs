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

        public UnlockCardsUseCase(CardProgressService cardProgressService)
        {
            _cardProgressService = cardProgressService ?? throw new ArgumentNullException(nameof(cardProgressService));
        }

        public async UniTask<UnlockCardsResult> ExecuteAsync(string eventId, IReadOnlyCollection<string> cardIds, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (cardIds == null || cardIds.Count == 0)
            {
                return UnlockCardsResult.Empty;
            }

            var data = await _cardProgressService.LoadAsync(eventId, ct);
            var unlockedBefore = new HashSet<string>(
                data.Cards.Where(card => card is { IsUnlocked: true }).Select(card => card.CardId),
                StringComparer.Ordinal);

            await _cardProgressService.UnlockCardsAsync(eventId, cardIds, ct);

            var newlyUnlocked = cardIds
                .Where(id => !string.IsNullOrEmpty(id) && !unlockedBefore.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            return newlyUnlocked.Length == 0
                ? UnlockCardsResult.Empty
                : new UnlockCardsResult(newlyUnlocked);
        }
    }
}
