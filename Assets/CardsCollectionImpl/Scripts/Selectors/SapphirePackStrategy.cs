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
    /// Data-driven pack selection strategy that reads its rules from a <see cref="PackRule"/>.
    /// <para>
    /// Supports:
    /// - Guaranteed minimum number of 3+ star cards
    /// - Missing-card boost (configurable percentages per consecutive pack without a missing card)
    /// </para>
    /// New pack types only need a <see cref="PackRule"/> entry — no additional strategy class required.
    /// </summary>
    public class SapphirePackStrategy : DefaultPackStrategy
    {
        private readonly PackRule _rule;
        private readonly PackOpeningHistory _packOpeningHistory;

        public SapphirePackStrategy(PackRule rule, PackOpeningHistory packOpeningHistory)
        {
            _rule = rule ?? throw new System.ArgumentNullException(nameof(rule));
            _packOpeningHistory = packOpeningHistory;
        }

        public override async UniTask<List<string>> SelectCardsAsync(
            CardPack pack, 
            List<CardDefinition> allCards, 
            PackSelectionContext context,
            CancellationToken ct = default)
        {
            await UniTask.Yield(ct);

            if (allCards == null || allCards.Count == 0)
            {
                Debug.LogWarning("[SapphirePackStrategy] No cards available");
                return new List<string>();
            }

            // Get missing cards if we need to check for missing card boost
            HashSet<string> missingCardIds = null;
            if (context.CardCollectionReader != null)
            {
                missingCardIds = await context.CardCollectionReader.GetMissingCardIdsAsync(allCards, ct);
            }

            var cardCount = pack.CardCount;
            var selectedCardIds = new List<string>(cardCount);
            var selectedCards = new List<CardDefinition>(cardCount);
            var remainingCards = new List<CardDefinition>(allCards);

            // Track if we've met the minimum 3+ star requirement
            int cardsWith3PlusStars = 0;
            bool hasMissingCard = false;

            for (int i = 0; i < cardCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                // Update available cards (remove already selected ones)
                var availableCardsForSelection = remainingCards.Where(c => !selectedCardIds.Contains(c.Id)).ToList();
                
                if (availableCardsForSelection.Count == 0)
                {
                    Debug.LogWarning("[SapphirePackStrategy] No more cards available to select");
                    break;
                }

                // Group cards by category from remaining cards
                var cardsByCategory = context.GroupCardsByCategory(availableCardsForSelection);
                var missingCardsByCategory = missingCardIds != null 
                    ? context.GroupCardsByCategory(availableCardsForSelection.Where(c => missingCardIds.Contains(c.Id)).ToList())
                    : new Dictionary<CardCategory, List<CardDefinition>>();

                CardDefinition selectedCard = null;

                // Check if we need to ensure at least one 3+ star card
                // Force it if we're on the last card and haven't met the requirement
                bool need3PlusStar = cardsWith3PlusStars < _rule.MinCardsWith3PlusStars
                    && (i == cardCount - 1 || (cardCount - i) <= (_rule.MinCardsWith3PlusStars - cardsWith3PlusStars));

                // Check if we should apply missing card boost
                bool shouldApplyMissingCardBoost = _rule.HasMissingCardBoost
                                                   && _packOpeningHistory != null
                                                   && missingCardIds != null 
                                                   && missingCardIds.Count > 0
                                                   && !hasMissingCard;

                if (shouldApplyMissingCardBoost && !need3PlusStar)
                {
                    var boostPercentage = _packOpeningHistory.GetMissingCardBoostPercentage(
                        pack.PackId, 
                        _rule.MissingCardBoostPercentages);
                    
                    if (boostPercentage > 0 && Random.Range(0f, 100f) < boostPercentage)
                    {
                        // Try to select a missing card
                        selectedCard = SelectMissingCardByProbability(missingCardsByCategory);
                        if (selectedCard != null)
                        {
                            hasMissingCard = true;
                        }
                    }
                }

                // If we didn't get a missing card from boost, or if we need a 3+ star card, select normally
                if (selectedCard == null)
                {
                    if (need3PlusStar)
                    {
                        // Force select a 3+ star card
                        selectedCard = SelectCardWithMinimumStars(cardsByCategory, 3);
                    }
                    else
                    {
                        selectedCard = SelectCardByProbability(cardsByCategory);
                    }
                }

                if (selectedCard != null)
                {
                    selectedCardIds.Add(selectedCard.Id);
                    selectedCards.Add(selectedCard);
                    
                    // Track 3+ star cards
                    if (selectedCard.Stars >= 3 && !selectedCard.PremiumCard)
                    {
                        cardsWith3PlusStars++;
                    }
                    else if (selectedCard.PremiumCard)
                    {
                        // Gold cards count as 3+ stars
                        cardsWith3PlusStars++;
                    }

                    // Track missing cards
                    if (missingCardIds != null && missingCardIds.Contains(selectedCard.Id))
                    {
                        hasMissingCard = true;
                    }
                }
                else
                {
                    // Fallback: if no card found in selected category, pick any random card from remaining
                    Debug.LogWarning($"[SapphirePackStrategy] No card found in selected category, falling back to random selection");
                    if (availableCardsForSelection.Count > 0)
                    {
                        var fallbackCard = availableCardsForSelection[Random.Range(0, availableCardsForSelection.Count)];
                        selectedCardIds.Add(fallbackCard.Id);
                        selectedCards.Add(fallbackCard);
                    }
                }
            }

            // Record pack opening history
            if (_packOpeningHistory != null)
            {
                _packOpeningHistory.RecordPackOpening(pack.PackId, hasMissingCard);
            }

            // Sort cards by stars (ascending), with PremiumCard (Gold) treated as highest value
            var sortedCards = selectedCards.OrderBy(c => c.PremiumCard ? 6 : c.Stars).ToList();
            
            return sortedCards.Select(c => c.Id).ToList();
        }


        private CardDefinition SelectMissingCardByProbability(Dictionary<CardCategory, List<CardDefinition>> missingCardsByCategory)
        {
            if (missingCardsByCategory == null || missingCardsByCategory.Count == 0)
                return null;

            // Use the same probability distribution but only from missing cards
            return SelectCardByProbability(missingCardsByCategory);
        }

        private CardDefinition SelectCardWithMinimumStars(Dictionary<CardCategory, List<CardDefinition>> cardsByCategory, int minStars)
        {
            // Try to find a card with at least minStars
            var eligibleCategories = new List<CardCategory>();
            
            if (minStars <= 3)
            {
                if (cardsByCategory.ContainsKey(CardCategory.Silver3Star) && cardsByCategory[CardCategory.Silver3Star].Count > 0)
                    eligibleCategories.Add(CardCategory.Silver3Star);
            }
            if (minStars <= 4)
            {
                if (cardsByCategory.ContainsKey(CardCategory.Silver4Star) && cardsByCategory[CardCategory.Silver4Star].Count > 0)
                    eligibleCategories.Add(CardCategory.Silver4Star);
            }
            if (minStars <= 5)
            {
                if (cardsByCategory.ContainsKey(CardCategory.Silver5Star) && cardsByCategory[CardCategory.Silver5Star].Count > 0)
                    eligibleCategories.Add(CardCategory.Silver5Star);
            }
            
            // Gold cards always count as 3+ stars
            if (cardsByCategory.ContainsKey(CardCategory.Gold) && cardsByCategory[CardCategory.Gold].Count > 0)
                eligibleCategories.Add(CardCategory.Gold);

            if (eligibleCategories.Count == 0)
            {
                // Fallback: return any available card
                foreach (var kvp in cardsByCategory)
                {
                    if (kvp.Value.Count > 0)
                    {
                        return kvp.Value[Random.Range(0, kvp.Value.Count)];
                    }
                }
                return null;
            }

            // Select from eligible categories using probability distribution
            var selectedCategory = eligibleCategories[Random.Range(0, eligibleCategories.Count)];
            var categoryCards = cardsByCategory[selectedCategory];
            return categoryCards[Random.Range(0, categoryCards.Count)];
        }
    }
}
