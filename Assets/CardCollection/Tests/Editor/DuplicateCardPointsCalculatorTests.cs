using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using NUnit.Framework;

namespace CardCollection.Tests
{
    public class DuplicateCardPointsCalculatorTests
    {
        [Test]
        public void Calculate_WhenCardsAreDuplicatesAcrossRarities_ReturnsExpectedTotal()
        {
            var calculator = new DuplicateCardPointsCalculator(
                new StubCardDefinitionProvider(CreateCardDefinitions(
                    ("1", 1, false),
                    ("2", 2, false),
                    ("3", 3, false),
                    ("4", 4, false),
                    ("5", 5, false),
                    ("6", 5, true))),
                new DefaultCardPointsCalculator());

            var result = calculator.Calculate(
                new[] { "1", "2", "3", "4", "5", "6" },
                new[]
                {
                    new CardProgressData { CardId = "1", IsUnlocked = true },
                    new CardProgressData { CardId = "2", IsUnlocked = true },
                    new CardProgressData { CardId = "3", IsUnlocked = true },
                    new CardProgressData { CardId = "4", IsUnlocked = true },
                    new CardProgressData { CardId = "5", IsUnlocked = true },
                    new CardProgressData { CardId = "6", IsUnlocked = true }
                });

            Assert.AreEqual(31, result.TotalPoints);
            Assert.IsTrue(result.HasPoints);
            Assert.AreEqual(6, result.AwardedCards.Count);
        }

        [Test]
        public void Calculate_WhenPackContainsOwnedAndNewCards_CountsOnlyOwnedDuplicates()
        {
            var calculator = new DuplicateCardPointsCalculator(
                new StubCardDefinitionProvider(CreateCardDefinitions(
                    ("5", 1, false),
                    ("10", 2, false),
                    ("150", 5, true))),
                new DefaultCardPointsCalculator());

            var result = calculator.Calculate(
                new[] { "5", "10", "150" },
                new[]
                {
                    new CardProgressData { CardId = "5", IsUnlocked = true },
                    new CardProgressData { CardId = "10", IsUnlocked = true },
                    new CardProgressData { CardId = "150", IsUnlocked = false }
                });

            Assert.AreEqual(3, result.TotalPoints);
            Assert.IsTrue(result.HasPoints);
            CollectionAssert.AreEquivalent(new[] { "5(+1)", "10(+2)" }, result.AwardedCards.ToArray());
        }

        private static List<CardDefinition> CreateCardDefinitions(params (string id, int stars, bool premiumCard)[] cards)
        {
            return cards.Select(card => new CardDefinition
            {
                Id = card.id,
                CardName = card.id,
                GroupType = "test",
                Stars = card.stars,
                PremiumCard = card.premiumCard,
                Icon = string.Empty
            }).ToList();
        }

        private sealed class StubCardDefinitionProvider : ICardDefinitionProvider
        {
            private readonly List<CardDefinition> _cardDefinitions;
            private readonly Dictionary<string, CardDefinition> _cardDefinitionsById;

            public StubCardDefinitionProvider(List<CardDefinition> cardDefinitions)
            {
                _cardDefinitions = cardDefinitions;
                _cardDefinitionsById = cardDefinitions.ToDictionary(card => card.Id, card => card);
            }

            public List<CardDefinition> GetCardDefinitions() => _cardDefinitions;
            public IReadOnlyDictionary<string, CardDefinition> GetCardDefinitionsById() => _cardDefinitionsById;
        }
    }
}
