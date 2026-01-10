using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
        /// Clears all caches. Useful when config data changes or for testing.
        /// </summary>
        public static void ClearCache()
        {
            _groupCardIdsCache.Clear();
            _cardsCache.Clear();
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
    }
}
