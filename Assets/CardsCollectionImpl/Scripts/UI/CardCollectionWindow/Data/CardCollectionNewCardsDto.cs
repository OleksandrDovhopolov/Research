using System.Collections.Generic;
using CardCollection.Core;
using Infrastructure;

namespace CardCollectionImpl
{
    public sealed class CardCollectionNewCardsDto
    {
        private readonly HashSet<string> _newCardIds;
        private readonly Dictionary<string, int> _newCardsByGroupType;
        
        public IReadOnlyCollection<string> NewCardIds => _newCardIds;
        
        private CardCollectionNewCardsDto(
            HashSet<string> newCardIds,
            Dictionary<string, int> newCardsByGroupType)
        {
            _newCardIds = newCardIds ?? new HashSet<string>();
            _newCardsByGroupType = newCardsByGroupType ?? new Dictionary<string, int>();
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

            var groupConfigs = CardCollectionConfigStorage.Instance.Get(groupType);
            foreach (var config in groupConfigs)
            {
                if (!string.IsNullOrEmpty(config?.Id))
                {
                    _newCardIds.Remove(config.Id);
                }
            }

            _newCardsByGroupType[groupType] = 0;
        }

        public static CardCollectionNewCardsDto Create(EventCardsSaveData eventCardsSaveData)
        {
            var newCardIds = new HashSet<string>();
            var newCardsByGroupType = new Dictionary<string, int>();

            if (eventCardsSaveData?.Cards == null || eventCardsSaveData.Cards.Count == 0)
            {
                return new CardCollectionNewCardsDto(newCardIds, newCardsByGroupType);
            }

            var groupTypeByCardId = new Dictionary<string, string>();
            foreach (var config in CardCollectionConfigStorage.Instance.Data)
            {
                if (!string.IsNullOrEmpty(config?.Id) && !string.IsNullOrEmpty(config.GroupType))
                {
                    groupTypeByCardId[config.Id] = config.GroupType;
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

            return new CardCollectionNewCardsDto(newCardIds, newCardsByGroupType);
        }
    }
}
