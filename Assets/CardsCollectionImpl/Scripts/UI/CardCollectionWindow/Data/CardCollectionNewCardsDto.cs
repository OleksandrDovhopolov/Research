using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionNewCardsDto
    {
        private readonly HashSet<string> _newCardIds;
        private readonly Dictionary<string, int> _newCardsByGroupType;
        private readonly IReadOnlyList<CardConfig> _cardConfigs;
        
        public IReadOnlyCollection<string> NewCardIds => _newCardIds;
        
        private CardCollectionNewCardsDto(
            HashSet<string> newCardIds,
            Dictionary<string, int> newCardsByGroupType,
            IReadOnlyList<CardConfig> data)
        {
            _newCardIds = newCardIds ?? new HashSet<string>();
            _newCardsByGroupType = newCardsByGroupType ?? new Dictionary<string, int>();
            _cardConfigs = data ?? new List<CardConfig>();
        }

        public bool IsNew(string cardId)
        {
            return !string.IsNullOrEmpty(cardId) && _newCardIds.Contains(cardId);
        }

        public int GetNewGroupAmount(string groupType)
        {
            if (string.IsNullOrEmpty(groupType))
            {
                return 0;
            }

            return _newCardsByGroupType.TryGetValue(groupType, out var amount) ? amount : 0;
        }

        public void MarkGroupAsSeen(string groupType)
        {
            if (string.IsNullOrEmpty(groupType))
            {
                return;
            }

            var groupConfigs = _cardConfigs.GetByGroupType(groupType);
            foreach (var config in groupConfigs)
            {
                if (!string.IsNullOrEmpty(config?.id))
                {
                    _newCardIds.Remove(config.id);
                }
            }

            _newCardsByGroupType[groupType] = 0;
        }

        public static CardCollectionNewCardsDto Create(EventCardsSaveData eventCardsSaveData, IReadOnlyList<CardConfig> data)
        {
            var newCardIds = new HashSet<string>();
            var newCardsByGroupType = new Dictionary<string, int>();

            if (eventCardsSaveData?.Cards == null || eventCardsSaveData.Cards.Count == 0)
            {
                return new CardCollectionNewCardsDto(newCardIds, newCardsByGroupType, data);
            }

            var groupTypeByCardId = new Dictionary<string, string>();
            foreach (var config in data)
            {
                if (!string.IsNullOrEmpty(config?.id) && !string.IsNullOrEmpty(config.groupType))
                {
                    groupTypeByCardId[config.id] = config.groupType;
                }
            }

            foreach (var cardData in eventCardsSaveData.Cards)
            {
                if (cardData == null || !cardData.IsUnlocked || !cardData.IsNew || string.IsNullOrEmpty(cardData.CardId))
                {
                    continue;
                }

                if (!newCardIds.Add(cardData.CardId))
                {
                    continue;
                }

                if (!groupTypeByCardId.TryGetValue(cardData.CardId, out var groupType))
                {
                    continue;
                }

                newCardsByGroupType.TryGetValue(groupType, out var currentAmount);
                newCardsByGroupType[groupType] = currentAmount + 1;
            }

            return new CardCollectionNewCardsDto(newCardIds, newCardsByGroupType, data);
        }
    }
}
