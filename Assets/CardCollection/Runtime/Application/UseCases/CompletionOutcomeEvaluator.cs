using System;
using System.Collections.Generic;
using System.Linq;

namespace CardCollection.Core
{
    internal readonly struct CompletionOutcome
    {
        public IReadOnlyList<string> NewlyCompletedGroupIds { get; }
        public bool CollectionCompleted { get; }

        public CompletionOutcome(IReadOnlyList<string> newlyCompletedGroupIds, bool collectionCompleted)
        {
            NewlyCompletedGroupIds = newlyCompletedGroupIds ?? Array.Empty<string>();
            CollectionCompleted = collectionCompleted;
        }
    }

    internal static class CompletionOutcomeEvaluator
    {
        public static CompletionOutcome Evaluate(
            IReadOnlyCollection<CardDefinition> cardDefinitions,
            IReadOnlyCollection<string> unlockedBefore,
            IReadOnlyCollection<string> unlockedAfter)
        {
            if (cardDefinitions == null || cardDefinitions.Count == 0)
            {
                return new CompletionOutcome(Array.Empty<string>(), false);
            }

            var groups = BuildGroups(cardDefinitions);
            if (groups.Count == 0)
            {
                return new CompletionOutcome(Array.Empty<string>(), false);
            }

            var beforeSet = new HashSet<string>(unlockedBefore ?? Array.Empty<string>(), StringComparer.Ordinal);
            var afterSet = new HashSet<string>(unlockedAfter ?? Array.Empty<string>(), StringComparer.Ordinal);
            var newlyCompleted = new List<string>();
            var completedAfterCount = 0;

            foreach (var pair in groups)
            {
                var groupId = pair.Key;
                var groupCardIds = pair.Value;
                var wasCompleted = IsGroupCompleted(groupCardIds, beforeSet);
                var isCompleted = IsGroupCompleted(groupCardIds, afterSet);

                if (isCompleted)
                {
                    completedAfterCount++;
                    if (!wasCompleted)
                    {
                        newlyCompleted.Add(groupId);
                    }
                }
            }

            var collectionCompleted = completedAfterCount == groups.Count;
            return new CompletionOutcome(newlyCompleted, collectionCompleted);
        }

        private static Dictionary<string, HashSet<string>> BuildGroups(IReadOnlyCollection<CardDefinition> cardDefinitions)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var definition in cardDefinitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.GroupType) || string.IsNullOrEmpty(definition.Id))
                {
                    continue;
                }

                if (!result.TryGetValue(definition.GroupType, out var ids))
                {
                    ids = new HashSet<string>(StringComparer.Ordinal);
                    result[definition.GroupType] = ids;
                }

                ids.Add(definition.Id);
            }

            return result;
        }

        private static bool IsGroupCompleted(HashSet<string> groupCardIds, HashSet<string> unlocked)
        {
            return groupCardIds.Count > 0 && groupCardIds.All(unlocked.Contains);
        }
    }
}
