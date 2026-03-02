using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CardCollection.Core;
using Infrastructure;

namespace CardCollectionImpl
{
    public static class EventCardsSaveDataExtensions
    {
        private static readonly Dictionary<string, HashSet<string>> _groupCardIdsCache = new();
        
        private static readonly ConditionalWeakTable<EventCardsSaveData, Dictionary<string, List<CardProgressData>>> _cardsCache = new();

        private static HashSet<string> GetGroupCardIds(string groupType)
        {
            if (_groupCardIdsCache.TryGetValue(groupType, out var cachedIds))
                return cachedIds;

            var groupCardsConfig = CardCollectionConfigStorage.Instance.Get(groupType);
            var groupCardIds = new HashSet<string>(groupCardsConfig.Select(config => config.Id));
            _groupCardIdsCache[groupType] = groupCardIds;
            return groupCardIds;
        }

        public static List<CardProgressData> GetCardsByGroupType(this EventCardsSaveData eventCardsSaveData, string groupType)
        {
            if (eventCardsSaveData?.Cards == null)
                return new List<CardProgressData>();

            if (!_cardsCache.TryGetValue(eventCardsSaveData, out var instanceCache))
            {
                instanceCache = new Dictionary<string, List<CardProgressData>>();
                _cardsCache.Add(eventCardsSaveData, instanceCache);
            }

            if (instanceCache.TryGetValue(groupType, out var cachedCards))
                return cachedCards;

            var groupCardIds = GetGroupCardIds(groupType);
            var filteredCards = eventCardsSaveData.Cards.Where(card => groupCardIds.Contains(card.CardId)).ToList();
            instanceCache[groupType] = filteredCards;
            
            return filteredCards;
        }

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
        
        public static int GetGroupAmount(this EventCardsSaveData eventCardsSaveData, string groupType)
        {
            if (eventCardsSaveData?.Cards == null)
                return 0;

            var groupCards = eventCardsSaveData.GetCardsByGroupType(groupType);
            return groupCards.Count;
        }
        
        public static int GetNewGroupAmount(this EventCardsSaveData data, string groupType)
        {
            if (data?.Cards == null)
                return 0;

            var groupCards = data.GetCardsByGroupType(groupType);
            return groupCards.Count(card => card.IsUnlocked && card.IsNew);
        }
        
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
