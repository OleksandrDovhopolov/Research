using System.Collections.Generic;
using CardCollection.Core;

namespace core
{
    /// <summary>
    /// Context object passed to pack selection strategies.
    /// Contains services and helpers needed for card selection.
    /// </summary>
    public class PackSelectionContext
    {
        public ICardCollectionReader CardCollectionReader { get; set; }
        
        /// <summary>
        /// Helper to group cards by category (1-5 star silver, gold).
        /// </summary>
        public Dictionary<CardCategory, List<CardDefinition>> GroupCardsByCategory(List<CardDefinition> cards)
        {
            var grouped = new Dictionary<CardCategory, List<CardDefinition>>();

            foreach (var card in cards)
            {
                CardCategory category;
                
                if (card.PremiumCard)
                {
                    category = CardCategory.Gold;
                }
                else
                {
                    category = card.Stars switch
                    {
                        1 => CardCategory.Silver1Star,
                        2 => CardCategory.Silver2Star,
                        3 => CardCategory.Silver3Star,
                        4 => CardCategory.Silver4Star,
                        5 => CardCategory.Silver5Star,
                        _ => CardCategory.Unknown
                    };
                }

                if (category != CardCategory.Unknown)
                {
                    if (!grouped.ContainsKey(category))
                    {
                        grouped[category] = new List<CardDefinition>();
                    }
                    grouped[category].Add(card);
                }
            }

            return grouped;
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
