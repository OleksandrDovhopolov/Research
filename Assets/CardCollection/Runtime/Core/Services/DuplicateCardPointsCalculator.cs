using System;
using System.Collections.Generic;

namespace CardCollection.Core
{
    public interface IDuplicateCardPointsCalculator
    {
        DuplicateCardPointsCalculation Calculate(
            IReadOnlyList<string> openedCardIds,
            IReadOnlyCollection<CardProgressData> openedCardsProgress);
    }

    public sealed class DuplicateCardPointsCalculation
    {
        public static readonly DuplicateCardPointsCalculation Empty = new(0, Array.Empty<string>());

        public int TotalPoints { get; }
        public IReadOnlyList<string> AwardedCards { get; }
        public bool HasPoints => TotalPoints > 0;

        public DuplicateCardPointsCalculation(int totalPoints, IReadOnlyList<string> awardedCards)
        {
            TotalPoints = totalPoints;
            AwardedCards = awardedCards ?? Array.Empty<string>();
        }
    }

    public sealed class DuplicateCardPointsCalculator : IDuplicateCardPointsCalculator
    {
        private readonly ICardDefinitionProvider _cardDefinitionProvider;
        private readonly ICardPointsCalculator _cardPointsCalculator;

        public DuplicateCardPointsCalculator(ICardDefinitionProvider cardDefinitionProvider, ICardPointsCalculator cardPointsCalculator)
        {
            _cardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
            _cardPointsCalculator = cardPointsCalculator ?? throw new ArgumentNullException(nameof(cardPointsCalculator));
        }

        public DuplicateCardPointsCalculation Calculate(
            IReadOnlyList<string> openedCardIds,
            IReadOnlyCollection<CardProgressData> openedCardsProgress)
        {
            if (openedCardIds == null || openedCardIds.Count == 0)
            {
                return DuplicateCardPointsCalculation.Empty;
            }

            if (openedCardsProgress == null || openedCardsProgress.Count == 0)
            {
                return DuplicateCardPointsCalculation.Empty;
            }

            var cardDefinitionsById = _cardDefinitionProvider.GetCardDefinitionsById();
            if (cardDefinitionsById == null || cardDefinitionsById.Count == 0)
            {
                return DuplicateCardPointsCalculation.Empty;
            }

            var unlockedCardIds = new HashSet<string>();
            foreach (var cardProgress in openedCardsProgress)
            {
                if (cardProgress is { IsUnlocked: true } && !string.IsNullOrEmpty(cardProgress.CardId))
                {
                    unlockedCardIds.Add(cardProgress.CardId);
                }
            }

            if (unlockedCardIds.Count == 0)
            {
                return DuplicateCardPointsCalculation.Empty;
            }

            var totalPoints = 0;
            var awardedCards = new List<string>();

            foreach (var cardId in openedCardIds)
            {
                if (string.IsNullOrEmpty(cardId) || !unlockedCardIds.Contains(cardId))
                {
                    continue;
                }

                if (!cardDefinitionsById.TryGetValue(cardId, out var cardDefinition))
                {
                    continue;
                }

                var pointsForCard = _cardPointsCalculator.GetPoints(cardDefinition.Stars, cardDefinition.PremiumCard);
                if (pointsForCard <= 0)
                {
                    continue;
                }

                totalPoints += pointsForCard;
                awardedCards.Add($"{cardId}(+{pointsForCard})");
            }

            return totalPoints > 0
                ? new DuplicateCardPointsCalculation(totalPoints, awardedCards)
                : DuplicateCardPointsCalculation.Empty;
        }
    }
}
