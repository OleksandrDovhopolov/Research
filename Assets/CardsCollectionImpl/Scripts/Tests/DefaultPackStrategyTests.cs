using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CardCollectionImpl
{
    public class DefaultPackStrategyTests
    {
        private static CardPack CreatePack(string packId, int cardCount)
        {
            return new CardPack(new CardPackConfig
            {
                packId = packId,
                packName = packId,
                cardCount = cardCount,
            });
        }

        private static List<CardDefinition> CreateCards(params (string id, int stars, bool premium)[] cards)
        {
            return cards.Select(c => new CardDefinition
            {
                Id = c.id,
                Stars = c.stars,
                PremiumCard = c.premium,
                CardName = c.id,
                GroupType = "test",
                Icon = ""
            }).ToList();
        }

        [UnityTest]
        public IEnumerator SelectCardsAsync_ReturnsCardCount_AndNoDuplicates()
        {
            var strategy = new DefaultPackStrategy();
            var context = new PackSelectionContext();

            var pack = CreatePack("default", 6);
            var allCards = CreateCards(
                ("c1", 1, false), ("c2", 1, false), ("c3", 2, false), ("c4", 2, false),
                ("c5", 3, false), ("c6", 3, false), ("c7", 4, false), ("c8", 4, false),
                ("c9", 5, false), ("c10", 5, false)
            );

            List<string> ids = null;
            yield return strategy.SelectCardsAsync(pack, allCards, context).ToCoroutine(result => ids = result);

            Assert.AreEqual(6, ids.Count);
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Selector should not return duplicate card IDs within one pack.");
        }

        [UnityTest]
        public IEnumerator SelectCardsAsync_WhenCardCountExceedsAllCards_ReturnsAllCardsCount()
        {
            var strategy = new DefaultPackStrategy();
            var context = new PackSelectionContext();

            var pack = CreatePack("default", 10);
            var allCards = CreateCards(
                ("c1", 1, false), ("c2", 2, false), ("c3", 3, false), ("c4", 4, false)
            );

            List<string> ids = null;
            yield return strategy.SelectCardsAsync(pack, allCards, context).ToCoroutine(result => ids = result);

            Assert.AreEqual(4, ids.Count);
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
        }
    }
}
