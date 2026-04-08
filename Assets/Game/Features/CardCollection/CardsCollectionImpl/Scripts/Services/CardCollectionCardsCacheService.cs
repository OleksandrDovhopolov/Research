using System.Collections.Generic;
using CardCollection.Core;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionCardsCacheService : ICardCollectionCacheService
    {
        private readonly Dictionary<string, HashSet<string>> _groupIdToCardIds = new();
        private readonly Dictionary<string, CardConfig> _configById = new();
        
        private readonly ICardPointsCalculator _pointsCalculator;

        public CardCollectionCardsCacheService(ICardPointsCalculator pointsCalculator)
        {
            _pointsCalculator = pointsCalculator;
        }

        public void Initialize(IReadOnlyList<CardConfig> configs)
        {
            _configById.Clear();
            _groupIdToCardIds.Clear();

            foreach (var config in configs)
            {
                if (config == null) continue;

                _configById[config.id] = config;

                if (!_groupIdToCardIds.TryGetValue(config.groupType, out var hashSet))
                {
                    hashSet = new HashSet<string>();
                    _groupIdToCardIds[config.groupType] = hashSet;
                }
                hashSet.Add(config.id);
            }
        }

        public IEnumerable<CardProgressData> GetCardsByGroupType(EventCardsSaveData saveData, string groupType)
        {
            if (saveData?.Cards == null || !_groupIdToCardIds.TryGetValue(groupType, out var groupIds))
                yield break;

            foreach (var card in saveData.Cards)
            {
                if (groupIds.Contains(card.CardId))
                    yield return card;
            }
        }
        
        public int GetGroupAmount(EventCardsSaveData saveData, string groupType)
        {
            if (saveData?.Cards == null || !_groupIdToCardIds.TryGetValue(groupType, out var groupIds))
                return 0;

            int count = 0;
            foreach (var card in saveData.Cards)
            {
                if (groupIds.Contains(card.CardId))
                    count++;
            }
            return count;
        }
        
        public int GetCollectedGroupAmount(EventCardsSaveData saveData, string groupType)
        {
            if (saveData?.Cards == null || !_groupIdToCardIds.TryGetValue(groupType, out var groupIds))
                return 0;

            int count = 0;
            foreach (var card in saveData.Cards)
            {
                if (card.IsUnlocked && groupIds.Contains(card.CardId))
                    count++;
            }
            return count;
        }

        public List<NewCardDisplayData> ToNewCardDisplayData(List<CardProgressData> cardsData)
        {
            var result = new List<NewCardDisplayData>(cardsData?.Count ?? 0);
            if (cardsData == null) return result;

            foreach (var cardData in cardsData)
            {
                if (_configById.TryGetValue(cardData.CardId, out var config))
                {
                    var points = _pointsCalculator.GetPoints(config.stars, config.premiumCard);
                    result.Add(new NewCardDisplayData(config, cardData.IsUnlocked, cardData.IsNew, points));
                }
                else
                {
                    Debug.LogWarning($"[CardCache] Config not found for card: {cardData.CardId}");
                }
            }

            return result;
        }
    }
}