using System;
using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public static class CardCollectionDataUtils 
    {
        public static int GetCollectedCardsAmount(this EventCardsSaveData eventCardsSaveData)
        {
            if (eventCardsSaveData?.Cards == null)
                return 0;

            return eventCardsSaveData.Cards.Count(card => card.IsUnlocked);
        }
        
        public static List<CardConfig> GetByGroupType(this IReadOnlyList<CardConfig> data, string groupType)
        {
            var configs = data.Where(config => groupType == config.groupType).ToList();
            return configs;
        }
        
        public static CardConfig GetById(this IReadOnlyList<CardConfig> data, string cardId)
        {
            var configs = data.FirstOrDefault(config => cardId == config.id);
            if (configs == null)
            {
                throw new InvalidOperationException("Failed to find config with id " + cardId);
            }
            
            return configs;
        }
    }
}