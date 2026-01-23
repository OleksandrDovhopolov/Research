using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CardCollection.Tests
{
    public class CardCollectionTests
    {
        private CardPackService service;
        private MockCardPackProvider mockProvider;

        [SetUp]
        public void Setup()
        {
            mockProvider = new MockCardPackProvider();
            service = new CardPackService(mockProvider);
        }

        [TearDown]
        public void Teardown()
        {
            service?.Dispose();
        }

        [UnityTest]
        public IEnumerator InitializeService_LoadsPacks()
        {
            yield return service.InitializeAsync().ToCoroutine();

            Assert.IsTrue(service.IsInitialized);
            var packs = service.GetAllPacks();
            Assert.Greater(packs.Count, 0);
        }

        [UnityTest]
        public IEnumerator GetAllPacks_ReturnsCorrectCount()
        {
            yield return service.InitializeAsync().ToCoroutine();

            var packs = service.GetAllPacks();

            Assert.AreEqual(mockProvider.GetTestPacks().Count, packs.Count);
        }

        [UnityTest]
        public IEnumerator GetPackById_ReturnsCorrectPack()
        {
            yield return service.InitializeAsync().ToCoroutine();
            var testPackId = "pack_2";

            var pack = service.GetPackById(testPackId);

            Assert.IsNotNull(pack);
            Assert.AreEqual(testPackId, pack.PackId);
        }

        [UnityTest]
        public IEnumerator GetPacksByCardCount_FiltersCorrectly()
        {
            yield return service.InitializeAsync().ToCoroutine();

            var packs = service.GetPacksByCardCount(3);

            Assert.IsTrue(packs.Count > 0);
            foreach (var pack in packs)
            {
                Assert.AreEqual(3, pack.CardCount);
            }
        }

        [UnityTest]
        public IEnumerator OnPackPurchased_IncrementsPurchaseCount()
        {
            yield return service.InitializeAsync().ToCoroutine();
            var packId = "pack_2";
            var pack = service.GetPackById(packId);
            var initialCount = pack.PurchaseCount;

            service.OnPackPurchased(packId);

            Assert.AreEqual(initialCount + 1, pack.PurchaseCount);
        }

        [UnityTest]
        public IEnumerator GetStatistics_ReturnsCorrectValues()
        {
            yield return service.InitializeAsync().ToCoroutine();
            service.OnPackPurchased("pack_2");
            service.OnPackPurchased("pack_3");

            var (total, available, purchases) = service.GetStatistics();

            Assert.Greater(total, 0);
            Assert.LessOrEqual(available, total);
            Assert.AreEqual(2, purchases);
        }

        private class MockCardPackProvider : ICardPackProvider
        {
            public async UniTask<List<CardPackConfig>> GetCardPacksAsync()
            {
                return GetTestPacks();
            }

            public async UniTask<CardPackConfig> GetCardPackByIdAsync(string packId)
            {
                var packs = GetTestPacks();
                return packs.Find(p => p.packId == packId);
            }

            public List<CardPackConfig> GetTestPacks()
            {
                return new List<CardPackConfig>
                {
                    new CardPackConfig
                    {
                        packId = "pack_2",
                        packName = "Test Pack 2",
                        cardCount = 2,
                        softCurrencyCost = 100,
                        hardCurrencyCost = 0,
                        availableCardRarities = new List<string> { "common", "rare" }
                    },
                    new CardPackConfig
                    {
                        packId = "pack_3",
                        packName = "Test Pack 3",
                        cardCount = 3,
                        softCurrencyCost = 200,
                        hardCurrencyCost = 0,
                        availableCardRarities = new List<string> { "common", "rare", "epic" }
                    },
                    new CardPackConfig
                    {
                        packId = "pack_limited",
                        packName = "Limited Pack",
                        cardCount = 5,
                        softCurrencyCost = 0,
                        hardCurrencyCost = 99,
                        availableCardRarities = new List<string> { "epic", "legendary" }
                    }
                };
            }
        }
    }

    public static class AsyncTestExtensions
    {
        public static IEnumerator ToCoroutine(this Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception;
            }
        }

        public static IEnumerator ToCoroutine<T>(this UniTask<T> task, System.Action<T> onResult)
        {
            var t = task.AsTask();
            while (!t.IsCompleted)
            {
                yield return null;
            }

            if (t.IsFaulted)
            {
                throw t.Exception;
            }

            onResult?.Invoke(t.Result);
        }
    }
}