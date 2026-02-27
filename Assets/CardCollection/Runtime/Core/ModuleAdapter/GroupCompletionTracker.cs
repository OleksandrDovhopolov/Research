using System.Collections.Generic;

namespace CardCollection.Core
{
    public class GroupCompletionTracker
    {
        private readonly Dictionary<string, HashSet<string>> _groupCardIds = new();
        private readonly Dictionary<string, string> _groupByCardId = new();
        private readonly HashSet<string> _unlockedCardIds = new();
        private readonly HashSet<string> _completedGroupIds = new();

        public GroupCompletionTracker(List<CardDefinition> allDefinitions, EventCardsSaveData progressData)
        {
            BuildGroupCache(allDefinitions);
            ResetFromProgress(progressData);
        }

        public void ResetFromProgress(EventCardsSaveData progressData)
        {
            _unlockedCardIds.Clear();
            _completedGroupIds.Clear();

            if (progressData?.Cards != null)
            {
                foreach (var card in progressData.Cards)
                {
                    if (card is { IsUnlocked: true } && !string.IsNullOrEmpty(card.CardId))
                    {
                        _unlockedCardIds.Add(card.CardId);
                    }
                }
            }

            foreach (var pair in _groupCardIds)
            {
                var groupCardIds = pair.Value;
                if (groupCardIds.Count == 0)
                {
                    continue;
                }

                var isCompleted = true;
                foreach (var cardId in groupCardIds)
                {
                    if (!_unlockedCardIds.Contains(cardId))
                    {
                        isCompleted = false;
                        break;
                    }
                }

                if (isCompleted)
                {
                    _completedGroupIds.Add(pair.Key);
                }
            }
        }
        
        public List<string> RegisterOpenedCards(IReadOnlyCollection<string> openedCardIds)
        {
            var affectedGroupIds = new HashSet<string>();
            foreach (var cardId in openedCardIds)
            {
                if (string.IsNullOrEmpty(cardId))
                {
                    continue;
                }

                // Only newly unlocked cards can complete a group.
                if (!_unlockedCardIds.Add(cardId))
                {
                    continue;
                }

                if (_groupByCardId.TryGetValue(cardId, out var groupId))
                {
                    affectedGroupIds.Add(groupId);
                }
            }

            var newlyCompleted = new List<string>();
            foreach (var groupId in affectedGroupIds)
            {
                if (_completedGroupIds.Contains(groupId) || !_groupCardIds.TryGetValue(groupId, out var groupCardIds))
                {
                    continue;
                }

                var isCompleted = true;
                foreach (var cardId in groupCardIds)
                {
                    if (!_unlockedCardIds.Contains(cardId))
                    {
                        isCompleted = false;
                        break;
                    }
                }

                if (isCompleted)
                {
                    _completedGroupIds.Add(groupId);
                    newlyCompleted.Add(groupId);
                }
            }

            return newlyCompleted;
        }
        
        private void BuildGroupCache(List<CardDefinition> allDefinitions)
        {
            _groupCardIds.Clear();
            _groupByCardId.Clear();

            if (allDefinitions == null)
            {
                return;
            }

            foreach (var definition in allDefinitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id) || string.IsNullOrEmpty(definition.GroupType))
                {
                    continue;
                }

                if (!_groupCardIds.TryGetValue(definition.GroupType, out var cardIds))
                {
                    cardIds = new HashSet<string>();
                    _groupCardIds[definition.GroupType] = cardIds;
                }

                cardIds.Add(definition.Id);
                _groupByCardId[definition.Id] = definition.GroupType;
            }
        }
    }
}