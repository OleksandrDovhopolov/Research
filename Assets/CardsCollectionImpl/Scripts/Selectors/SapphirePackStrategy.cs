using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly JsonPackOpeningHistoryStorage _packOpeningHistoryStorage;
        private readonly SemaphoreSlim _initializeSemaphore = new(1, 1);
        private bool _isInitialized;

        public SapphirePackStrategy(
            PackRule rule,
            PackOpeningHistory packOpeningHistory,
            JsonPackOpeningHistoryStorage packOpeningHistoryStorage = null)
        {
            _rule = rule ?? throw new System.ArgumentNullException(nameof(rule));
            _packOpeningHistory = packOpeningHistory ?? new PackOpeningHistory();
            _packOpeningHistoryStorage = packOpeningHistoryStorage ?? new JsonPackOpeningHistoryStorage();
        }

        public override async UniTask<List<string>> SelectCardsAsync(
            CardPack pack, 
            List<CardDefinition> allCards, 
            PackSelectionContext context,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);
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
            var selectedCardIds = new HashSet<string>();
            var selectedCards = new List<CardDefinition>(cardCount);
            var remainingCards = new List<CardDefinition>(allCards);
            var cardsByCategory = context.GroupCardsByCategory(remainingCards);
            var missingCardsByCategory = BuildMissingCardsByCategory(remainingCards, missingCardIds, context);

            // Track if we've met the minimum 3+ star requirement
            int cardsWith3PlusStars = 0;
            bool hasMissingCard = false;

            for (int i = 0; i < cardCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                if (remainingCards.Count == 0)
                {
                    Debug.LogWarning("[SapphirePackStrategy] No more cards available to select");
                    break;
                }

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

                if (selectedCard == null)
                {
                    // Fallback: if no card found in selected category, pick any random card from remaining
                    Debug.LogWarning($"[SapphirePackStrategy] No card found in selected category, falling back to random selection");
                    selectedCard = remainingCards[Random.Range(0, remainingCards.Count)];
                }

                if (selectedCard != null && selectedCardIds.Add(selectedCard.Id))
                {
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

                    RemoveSelectedCardFromPools(selectedCard, remainingCards, cardsByCategory, missingCardsByCategory, context);
                }
            }

            // Record pack opening history
            if (_packOpeningHistory != null)
            {
                _packOpeningHistory.RecordPackOpening(pack.PackId, hasMissingCard);
                await PersistHistoryAsync(ct);
            }

            // Sort cards by stars (ascending), with PremiumCard (Gold) treated as highest value
            var sortedCards = selectedCards.OrderBy(c => c.PremiumCard ? 6 : c.Stars).ToList();
            
            return sortedCards.Select(c => c.Id).ToList();
        }

        private async UniTask EnsureInitializedAsync(CancellationToken ct)
        {
            if (_isInitialized)
            {
                return;
            }

            await _initializeSemaphore.WaitAsync(ct);
            try
            {
                if (_isInitialized)
                {
                    return;
                }

                await _packOpeningHistoryStorage.InitializeAsync(ct);
                var historyData = await _packOpeningHistoryStorage.LoadAsync(ct);
                _packOpeningHistory.LoadFromSaveData(historyData);
                _isInitialized = true;
            }
            finally
            {
                _initializeSemaphore.Release();
            }
        }

        private async UniTask PersistHistoryAsync(CancellationToken ct)
        {
            var historyData = _packOpeningHistory.ToSaveData();
            await _packOpeningHistoryStorage.SaveAsync(historyData, ct);
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

        private static Dictionary<CardCategory, List<CardDefinition>> BuildMissingCardsByCategory(
            List<CardDefinition> allCards,
            HashSet<string> missingCardIds, 
            PackSelectionContext context)
        {
            if (missingCardIds == null || missingCardIds.Count == 0)
                return null;

            var grouped = new Dictionary<CardCategory, List<CardDefinition>>();
            foreach (var card in allCards)
            {
                if (!missingCardIds.Contains(card.Id))
                    continue;

                if (!context.TryGetCardCategory(card, out var category))
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

        private static void RemoveSelectedCardFromPools(CardDefinition selectedCard,
            List<CardDefinition> remainingCards,
            Dictionary<CardCategory, List<CardDefinition>> cardsByCategory,
            Dictionary<CardCategory, List<CardDefinition>> missingCardsByCategory, PackSelectionContext context)
        {
            remainingCards.Remove(selectedCard);

            if (context.TryGetCardCategory(selectedCard, out var category))
            {
                RemoveCardFromCategory(cardsByCategory, category, selectedCard);
                RemoveCardFromCategory(missingCardsByCategory, category, selectedCard);
            }
        }

        private static void RemoveCardFromCategory(
            Dictionary<CardCategory, List<CardDefinition>> cardsByCategory,
            CardCategory category,
            CardDefinition card)
        {
            if (cardsByCategory == null)
                return;

            if (!cardsByCategory.TryGetValue(category, out var categoryCards))
                return;

            categoryCards.Remove(card);
            if (categoryCards.Count == 0)
            {
                cardsByCategory.Remove(category);
            }
        }
    }
}
