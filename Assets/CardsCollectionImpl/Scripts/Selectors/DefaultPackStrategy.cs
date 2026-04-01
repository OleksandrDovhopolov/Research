using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CardCollectionImpl
{
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
    
    /// <summary>
    /// Default pack selection strategy using base probability distribution:
    /// - 1-Star Silver: 33.67%
    /// - 2-Star Silver: 26.93%
    /// - 3-Star Silver: 17.83%
    /// - 4-Star Silver: 10.60%
    /// - 5-Star Silver: 5.99%
    /// - Gold (PremiumCard): 4.99%
    /// </summary>
    public class DefaultPackStrategy : IPackSelectionStrategy
    {
        // Probability thresholds (cumulative)
        private const float Probability1StarSilver = 33.67f;
        private const float Probability2StarSilver = 33.67f + 26.93f; // 60.60%
        private const float Probability3StarSilver = 60.60f + 17.83f; // 78.43%
        private const float Probability4StarSilver = 78.43f + 10.60f; // 89.03%
        private const float Probability5StarSilver = 89.03f + 5.99f; // 95.02%

        public virtual async UniTask<List<string>> SelectCardsAsync(
            CardPack pack, 
            List<CardDefinition> allCards,
            CancellationToken ct = default)
        {
            await UniTask.Yield(ct);

            if (allCards == null || allCards.Count == 0)
            {
                Debug.LogWarning("[DefaultPackStrategy] No cards available");
                return new List<string>();
            }

            var cardCount = pack.CardCount;
            var selectedCards = new List<CardDefinition>(cardCount);
            var remainingCards = new List<CardDefinition>(allCards);

            for (int i = 0; i < cardCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                // Update available cards (remove already selected ones)
                var selectedCardIdsSet = new HashSet<string>(selectedCards.Select(c => c.Id));
                var availableCardsForSelection = remainingCards.Where(c => !selectedCardIdsSet.Contains(c.Id)).ToList();
                
                if (availableCardsForSelection.Count == 0)
                {
                    Debug.LogWarning("[DefaultPackStrategy] No more cards available to select");
                    break;
                }

                // Group cards by category from remaining cards
                var cardsByCategory = GroupCardsByCategory(availableCardsForSelection);

                var selectedCard = SelectCardByProbability(cardsByCategory);

                if (selectedCard != null)
                {
                    selectedCards.Add(selectedCard);
                }
                else
                {
                    // Fallback: if no card found in selected category, pick any random card from remaining
                    Debug.LogWarning($"[DefaultPackStrategy] No card found in selected category, falling back to random selection");
                    if (availableCardsForSelection.Count > 0)
                    {
                        var fallbackCard = availableCardsForSelection[Random.Range(0, availableCardsForSelection.Count)];
                        selectedCards.Add(fallbackCard);
                    }
                }
            }

            return selectedCards.Select(c => c.Id).ToList();
        }

        protected CardDefinition SelectCardByProbability(Dictionary<CardCategory, List<CardDefinition>> cardsByCategory)
        {
            var randomValue = Random.Range(0f, 100f);
            CardCategory selectedCategory;

            if (randomValue < Probability1StarSilver)
            {
                selectedCategory = CardCategory.Silver1Star;
            }
            else if (randomValue < Probability2StarSilver)
            {
                selectedCategory = CardCategory.Silver2Star;
            }
            else if (randomValue < Probability3StarSilver)
            {
                selectedCategory = CardCategory.Silver3Star;
            }
            else if (randomValue < Probability4StarSilver)
            {
                selectedCategory = CardCategory.Silver4Star;
            }
            else if (randomValue < Probability5StarSilver)
            {
                selectedCategory = CardCategory.Silver5Star;
            }
            else
            {
                selectedCategory = CardCategory.Gold;
            }

            // Get a random card from the selected category
            if (cardsByCategory.TryGetValue(selectedCategory, out var categoryCards) && categoryCards.Count > 0)
            {
                return categoryCards[Random.Range(0, categoryCards.Count)];
            }

            // If category is empty, try to find any available category
            foreach (var kvp in cardsByCategory)
            {
                if (kvp.Value.Count > 0)
                {
                    return kvp.Value[Random.Range(0, kvp.Value.Count)];
                }
            }

            return null;
        }
        
        /// <summary>
        /// Helper to group cards by category (1-5 star silver, gold).
        /// </summary>
        private Dictionary<CardCategory, List<CardDefinition>> GroupCardsByCategory(List<CardDefinition> cards)
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
        
        private bool TryGetCardCategory(CardDefinition card, out CardCategory category)
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
}
