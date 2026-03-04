using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    /// <summary>
    /// Context object passed to pack selection strategies.
    /// Extends Core <see cref="CardSelectionContext"/> with implementation-specific helpers
    /// such as card category grouping.
    /// </summary>
    public class PackSelectionContext : CardSelectionContext
    {
        public PackSelectionContext(ICardCollectionReader cardCollectionReader = null)
            : base(cardCollectionReader)
        {
        }

        /// <summary>
        /// Helper to group cards by category (1-5 star silver, gold).
        /// </summary>
        public Dictionary<CardCategory, List<CardDefinition>> GroupCardsByCategory(List<CardDefinition> cards)
        {
            var grouped = new Dictionary<CardCategory, List<CardDefinition>>();

            foreach (var card in cards)
            {
                if (!TryGetCardCategory(card, out var category))
                    continue;

                if (!grouped.TryGetValue(category, out var categoryCards))
                {
                    categoryCards = new List<CardDefinition>();
                    grouped[category] = categoryCards;
                }

                categoryCards.Add(card);
            }

            return grouped;
        }
        
        public bool TryGetCardCategory(CardDefinition card, out CardCategory category)
        {
            if (card.PremiumCard)
            {
                category = CardCategory.Gold;
                return true;
            }

            category = card.Stars switch
            {
                1 => CardCategory.Silver1Star,
                2 => CardCategory.Silver2Star,
                3 => CardCategory.Silver3Star,
                4 => CardCategory.Silver4Star,
                5 => CardCategory.Silver5Star,
                _ => CardCategory.Unknown
            };

            return category != CardCategory.Unknown;
        }
    }

    /// <summary>
    /// Card categories for probability-based selection.
    /// </summary>
    public enum CardCategory
    {
        Silver1Star,
        Silver2Star,
        Silver3Star,
        Silver4Star,
        Silver5Star,
        Gold,
        Unknown
    }
}
