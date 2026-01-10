using System.Collections.Generic;
using System.Linq;

namespace core
{
    public static class EventCardsSaveDataExtensions
    {
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

            var groupCardsConfig = CardCollectionConfigStorage.Instance.Get(groupType);
            var groupCardIds = new HashSet<string>(groupCardsConfig.Select(config => config.Id));
            
            return eventCardsSaveData.Cards.Where(card => groupCardIds.Contains(card.CardId)).ToList();
        }
    }
}
