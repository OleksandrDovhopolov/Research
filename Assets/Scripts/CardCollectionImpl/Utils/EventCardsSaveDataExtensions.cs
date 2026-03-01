using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CardCollection.Core;

namespace core
{
    public static class EventCardsSaveDataExtensions
    {
        // Cache for group card ID HashSets (static since config doesn't change)
        private static readonly Dictionary<string, HashSet<string>> _groupCardIdsCache = new();
        
        // Cache for filtered card lists per EventCardsSaveData instance
        // Using ConditionalWeakTable to avoid memory leaks when instances are garbage collected
        private static readonly ConditionalWeakTable<EventCardsSaveData, Dictionary<string, List<CardProgressData>>> _cardsCache = new();

        /// <summary>
        /// Gets or creates a cached HashSet of card IDs for the specified group type.
        /// </summary>
        private static HashSet<string> GetGroupCardIds(string groupType)
        {
            if (_groupCardIdsCache.TryGetValue(groupType, out var cachedIds))
                return cachedIds;

            var groupCardsConfig = CardCollectionConfigStorage.Instance.Get(groupType);
            var groupCardIds = new HashSet<string>(groupCardsConfig.Select(config => config.Id));
            _groupCardIdsCache[groupType] = groupCardIds;
            return groupCardIds;
        }

        /// <summary>
        /// Filters cards from EventCardsSaveData by the specified group type.
        /// </summary>
        /// <param name="eventCardsSaveData">The event cards save data to filter</param>
        /// <param name="groupType">The group type to filter by</param>
        /// <returns>List of CardProgressData matching the group type</returns>
        public static List<CardProgressData> GetCardsByGroupType(this EventCardsSaveData eventCardsSaveData, string groupType)
        {
            if (eventCardsSaveData?.Cards == null)
                return new List<CardProgressData>();

            // Try to get cached instance dictionary
            if (!_cardsCache.TryGetValue(eventCardsSaveData, out var instanceCache))
            {
                instanceCache = new Dictionary<string, List<CardProgressData>>();
                _cardsCache.Add(eventCardsSaveData, instanceCache);
            }

            // Check if we have cached result for this group type
            if (instanceCache.TryGetValue(groupType, out var cachedCards))
                return cachedCards;

            // Cache miss - compute and cache the result
            var groupCardIds = GetGroupCardIds(groupType);
            var filteredCards = eventCardsSaveData.Cards.Where(card => groupCardIds.Contains(card.CardId)).ToList();
            instanceCache[groupType] = filteredCards;
            
            return filteredCards;
        }

        /// <summary>
        /// Calculates the number of unlocked cards for the specified group type.
        /// </summary>
        /// <param name="eventCardsSaveData">The event cards save data</param>
        /// <param name="groupType">The group type to calculate unlocked cards for</param>
        /// <returns>The count of unlocked cards in the group</returns>
        public static int GetCollectedGroupAmount(this EventCardsSaveData eventCardsSaveData, string groupType)
        {
            if (eventCardsSaveData?.Cards == null)
                return 0;

            var groupCards = eventCardsSaveData.GetCardsByGroupType(groupType);
            return groupCards.Count(card => card.IsUnlocked);
        }
        
        public static int GetCollectedCardsAmount(this EventCardsSaveData eventCardsSaveData)
        {
            if (eventCardsSaveData?.Cards == null)
                return 0;

            return eventCardsSaveData.Cards.Count(card => card.IsUnlocked);
        }
        
        /// <summary>
        /// Calculates the number of cards for the specified group type.
        /// </summary>
        /// <param name="eventCardsSaveData">The event cards save data</param>
        /// <param name="groupType">The group type to calculate unlocked cards for</param>
        /// <returns>The count of unlocked cards in the group</returns>
        public static int GetGroupAmount(this EventCardsSaveData eventCardsSaveData, string groupType)
        {
            if (eventCardsSaveData?.Cards == null)
                return 0;

            var groupCards = eventCardsSaveData.GetCardsByGroupType(groupType);
            return groupCards.Count;
        }
        
        /// <summary>
        /// Calculates the number of new cards for the specified group type.
        /// </summary>
        /// <param name="data">The event cards save data</param>
        /// <param name="groupType">The group type to calculate new cards for</param>
        /// <returns>The count of unlocked cards in the group</returns>
        public static int GetNewGroupAmount(this EventCardsSaveData data, string groupType)
        {
            if (data?.Cards == null)
                return 0;

            var groupCards = data.GetCardsByGroupType(groupType);
            return groupCards.Count(card => card.IsUnlocked && card.IsNew);
        }
        
        /// <summary>
        /// Converts a list of CardProgressData to NewCardDisplayData.
        /// This hides CardProgressData internals from the view layer.
        /// Points for duplicate (non-new) cards are calculated via the provided <paramref name="pointsCalculator"/>.
        /// </summary>
        /// <param name="cardsData">List of CardProgressData to convert</param>
        /// <param name="pointsCalculator">Strategy used to calculate points per card</param>
        /// <returns>List of NewCardDisplayData</returns>
        public static List<NewCardDisplayData> ToNewCardDisplayData(this List<CardProgressData> cardsData)
        {
            if (cardsData == null || cardsData.Count == 0)
                return new List<NewCardDisplayData>();

            var result = new List<NewCardDisplayData>(cardsData.Count);
            
            foreach (var cardData in cardsData)
            {
                var config = CardCollectionConfigStorage.Instance.GetById(cardData.CardId);
                var points = CardsCollectionPointsCalculator.Instance.GetPoints(config.Stars, config.PremiumCard);
                result.Add(new NewCardDisplayData(config, cardData.IsUnlocked, cardData.IsNew, points));
            }
            
            return result;
        }
    }
}
