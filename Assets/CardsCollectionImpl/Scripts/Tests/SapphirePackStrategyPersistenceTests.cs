using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CardCollectionImpl
{
    public class SapphirePackStrategyPersistenceTests
    {
        private string _directoryName;
        private const string FileName = "history.json";

        [SetUp]
        public void SetUp()
        {
            _directoryName = $"pack_history_test_{Guid.NewGuid():N}";
        }

        [TearDown]
        public void TearDown()
        {
            var path = Path.Combine(Application.persistentDataPath, _directoryName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        [UnityTest]
        public IEnumerator Strategy_LoadsPersistedHistory_AndContinuesConsecutiveCounter()
        {
            var rule = new PackRule
            {
                PackId = "sapphire_pack",
                MinCardsWith3PlusStars = 0,
                HasMissingCardBoost = true,
                MissingCardBoostPercentages = new[] { 33f, 66f, 100f }
            };

            var pack = CreatePack("sapphire_pack", 1);
            var allCards = CreateCards(("c1", 1, false), ("c2", 2, false), ("c3", 3, false));
            var noMissingReader = new StubCardCollectionReader(new HashSet<string>());
            var context = new PackSelectionContext(noMissingReader);

            var strategyFirstSession = new SapphirePackStrategy(
                rule,
                new PackOpeningHistory(),
                new JsonPackOpeningHistoryStorage(_directoryName, FileName));

            yield return strategyFirstSession.SelectCardsAsync(pack, allCards, context).ToCoroutine();
            yield return strategyFirstSession.SelectCardsAsync(pack, allCards, context).ToCoroutine();

            PackOpeningHistorySaveData saveAfterFirstSession = null;
            var storageReader = new JsonPackOpeningHistoryStorage(_directoryName, FileName);
            yield return storageReader.InitializeAsync().ToCoroutine();
            yield return storageReader.LoadAsync().ToCoroutine(result => saveAfterFirstSession = result);

            var firstEntry = saveAfterFirstSession.Entries.FirstOrDefault(entry => entry.PackId == pack.PackId);
            Assert.NotNull(firstEntry);
            Assert.AreEqual(2, firstEntry.ConsecutivePacksWithoutMissingCard);
            Assert.AreEqual(2, firstEntry.TotalPacksOpened);

            var strategySecondSession = new SapphirePackStrategy(
                rule,
                new PackOpeningHistory(),
                new JsonPackOpeningHistoryStorage(_directoryName, FileName));

            yield return strategySecondSession.SelectCardsAsync(pack, allCards, context).ToCoroutine();

            PackOpeningHistorySaveData saveAfterSecondSession = null;
            var storageReaderAfterRestart = new JsonPackOpeningHistoryStorage(_directoryName, FileName);
            yield return storageReaderAfterRestart.InitializeAsync().ToCoroutine();
            yield return storageReaderAfterRestart.LoadAsync().ToCoroutine(result => saveAfterSecondSession = result);

            var secondEntry = saveAfterSecondSession.Entries.FirstOrDefault(entry => entry.PackId == pack.PackId);
            Assert.NotNull(secondEntry);
            Assert.AreEqual(3, secondEntry.ConsecutivePacksWithoutMissingCard);
            Assert.AreEqual(3, secondEntry.TotalPacksOpened);
        }

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
            return cards.Select(card => new CardDefinition
            {
                Id = card.id,
                Stars = card.stars,
                PremiumCard = card.premium,
                CardName = card.id,
                GroupType = "test",
                Icon = ""
            }).ToList();
        }

        private sealed class StubCardCollectionReader : ICardCollectionReader
        {
            private readonly HashSet<string> _missingCardIds;

            public StubCardCollectionReader(HashSet<string> missingCardIds)
            {
                _missingCardIds = missingCardIds ?? new HashSet<string>();
            }

            public UniTask<EventCardsSaveData> Load(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(new EventCardsSaveData());
            }

            public UniTask<HashSet<string>> GetMissingCardIdsAsync(List<CardDefinition> allCards, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(new HashSet<string>(_missingCardIds));
            }

            public UniTask<int> GetCollectionPoints()
            {
                return UniTask.FromResult(0);
            }
        }
    }
}
