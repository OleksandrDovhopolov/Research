using System.Collections.Generic;
using CardCollection.Core;
using NUnit.Framework;

namespace CardCollection.Tests
{
    public class GroupCompletionTrackerTests
    {
        [Test]
        public void Constructor_WhenGroupAlreadyComplete_DoesNotEmitCompletionAgain()
        {
            var tracker = new GroupCompletionTracker(
                CreateDefinitions(("a1", "group-a"), ("a2", "group-a")),
                CreateProgress(("a1", true), ("a2", true)));

            var completed = tracker.RegisterOpenedCards(new[] { "a2" });

            Assert.IsEmpty(completed);
        }

        [Test]
        public void RegisterOpenedCards_WhenLastCardInGroupIsOpened_ReturnsCompletedGroupId()
        {
            var tracker = new GroupCompletionTracker(
                CreateDefinitions(("a1", "group-a"), ("a2", "group-a")),
                CreateProgress(("a1", true), ("a2", false)));

            var completed = tracker.RegisterOpenedCards(new[] { "a2" });

            CollectionAssert.AreEquivalent(new[] { "group-a" }, completed);
        }

        [Test]
        public void RegisterOpenedCards_WhenCardAlreadyUnlocked_DoesNotReturnDuplicates()
        {
            var tracker = new GroupCompletionTracker(
                CreateDefinitions(("a1", "group-a"), ("a2", "group-a")),
                CreateProgress(("a1", true), ("a2", false)));

            var firstCompletion = tracker.RegisterOpenedCards(new[] { "a2" });
            var secondCompletion = tracker.RegisterOpenedCards(new[] { "a2" });

            CollectionAssert.AreEquivalent(new[] { "group-a" }, firstCompletion);
            Assert.IsEmpty(secondCompletion);
        }

        [Test]
        public void RegisterOpenedCards_IgnoresEmptyAndUnknownCardIds()
        {
            var tracker = new GroupCompletionTracker(
                CreateDefinitions(("a1", "group-a"), ("a2", "group-a")),
                CreateProgress(("a1", true), ("a2", false)));

            var completed = tracker.RegisterOpenedCards(new[] { "", null, "unknown" });

            Assert.IsEmpty(completed);
        }

        [Test]
        public void ResetFromProgress_RebuildsCompletionStateFromNewProgress()
        {
            var tracker = new GroupCompletionTracker(
                CreateDefinitions(("a1", "group-a"), ("a2", "group-a")),
                CreateProgress(("a1", true), ("a2", false)));

            var firstCompletion = tracker.RegisterOpenedCards(new[] { "a2" });
            tracker.ResetFromProgress(CreateProgress(("a1", false), ("a2", false)));
            var afterResetCompletion = tracker.RegisterOpenedCards(new[] { "a1", "a2" });

            CollectionAssert.AreEquivalent(new[] { "group-a" }, firstCompletion);
            CollectionAssert.AreEquivalent(new[] { "group-a" }, afterResetCompletion);
        }

        private static List<CardDefinition> CreateDefinitions(params (string id, string groupId)[] cards)
        {
            var definitions = new List<CardDefinition>(cards.Length);
            foreach (var card in cards)
            {
                definitions.Add(new CardDefinition
                {
                    Id = card.id,
                    GroupType = card.groupId,
                    CardName = card.id,
                    Icon = string.Empty
                });
            }

            return definitions;
        }

        private static EventCardsSaveData CreateProgress(params (string cardId, bool isUnlocked)[] cards)
        {
            var progress = new EventCardsSaveData { EventId = "event" };
            foreach (var card in cards)
            {
                progress.Cards.Add(new CardProgressData
                {
                    CardId = card.cardId,
                    IsUnlocked = card.isUnlocked
                });
            }

            return progress;
        }
    }
}
