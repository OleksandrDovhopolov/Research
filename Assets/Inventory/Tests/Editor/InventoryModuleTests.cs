using System;
using System.Threading;
using Inventory.API;
using Inventory.Implementation.Services;
using NUnit.Framework;
using UnityEngine;

namespace Inventory.Tests.Editor
{
    public sealed class InventoryModuleTests
    {
        private const string OwnerId = "player_1";
        private TestItemCategory _regularCategory;
        private TestItemCategory _cardPackCategory;
        private TestItemCategory _alchemyCategory;

        [SetUp]
        public void SetUp()
        {
            _regularCategory = CreateCategory(InventoryBuiltInCategoryIds.Regular, "Regular");
            _cardPackCategory = CreateCategory(InventoryBuiltInCategoryIds.CardPack, "Card packs");
            _alchemyCategory = CreateCategory("alchemy", "Alchemy");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_regularCategory);
            UnityEngine.Object.DestroyImmediate(_cardPackCategory);
            UnityEngine.Object.DestroyImmediate(_alchemyCategory);
        }

        [Test]
        public void AddItem_StacksByOwnerItemAndCategory()
        {
            var service = new InventoryModuleService();

            service.AddItemAsync(new InventoryItemDelta(OwnerId, "wood",  2, _regularCategory))
                .GetAwaiter()
                .GetResult();
            service.AddItemAsync(new InventoryItemDelta(OwnerId, "wood",  3, _regularCategory))
                .GetAwaiter()
                .GetResult();

            var items = service.GetItemsAsync(OwnerId, _regularCategory.CategoryId)
                .GetAwaiter()
                .GetResult();

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].StackCount, Is.EqualTo(5));
        }

        [Test]
        public void RemoveItem_DecrementsAndDeletesEntityAtZero()
        {
            var service = new InventoryModuleService();

            service.AddItemAsync(new InventoryItemDelta(OwnerId, "energy", 5, _regularCategory))
                .GetAwaiter()
                .GetResult();
            service.RemoveItemAsync(new InventoryItemDelta(OwnerId, "energy", 2, _regularCategory))
                .GetAwaiter()
                .GetResult();

            var afterPartialRemove = service.GetItemsAsync(OwnerId, _regularCategory.CategoryId)
                .GetAwaiter()
                .GetResult();
            Assert.That(afterPartialRemove.Count, Is.EqualTo(1));
            Assert.That(afterPartialRemove[0].StackCount, Is.EqualTo(3));

            service.RemoveItemAsync(new InventoryItemDelta(OwnerId, "energy",  3, _regularCategory))
                .GetAwaiter()
                .GetResult();

            var afterFullRemove = service.GetItemsAsync(OwnerId, _regularCategory.CategoryId)
                .GetAwaiter()
                .GetResult();
            Assert.That(afterFullRemove.Count, Is.EqualTo(0));
        }

        [Test]
        public void QuerySystem_FiltersByCategory_AndPreservesCardPackMetadata()
        {
            var service = new InventoryModuleService();

            service.AddItemAsync(new InventoryItemDelta(OwnerId, "gold",  10, _regularCategory))
                .GetAwaiter()
                .GetResult();
            
            service.AddItemAsync(new InventoryItemDelta(
                    OwnerId,
                    "pack_blue",
                    1,
                    _cardPackCategory))
                .GetAwaiter()
                .GetResult();

            var regular = service.GetItemsAsync(OwnerId, _regularCategory.CategoryId)
                .GetAwaiter()
                .GetResult();
            var packs = service.GetItemsAsync(OwnerId, _cardPackCategory.CategoryId)
                .GetAwaiter()
                .GetResult();

            Assert.That(regular.Count, Is.EqualTo(1));
            Assert.That(regular[0].ItemId, Is.EqualTo("gold"));

            Assert.That(packs.Count, Is.EqualTo(1));
            Assert.That(packs[0].ItemId, Is.EqualTo("pack_blue"));
        }

        [Test]
        public void AddItem_AllowsNewCategoryWithoutCoreChanges()
        {
            var service = new InventoryModuleService();

            service.AddItemAsync(new InventoryItemDelta(OwnerId, "dust", 4, _alchemyCategory))
                .GetAwaiter()
                .GetResult();

            var alchemyItems = service.GetItemsAsync(OwnerId, _alchemyCategory.CategoryId)
                .GetAwaiter()
                .GetResult();

            Assert.That(alchemyItems.Count, Is.EqualTo(1));
            Assert.That(alchemyItems[0].ItemId, Is.EqualTo("dust"));
            Assert.That(alchemyItems[0].StackCount, Is.EqualTo(4));
            Assert.That(alchemyItems[0].CategoryId, Is.EqualTo(_alchemyCategory.CategoryId));
        }

        [Test]
        public void AddItemAsync_ThrowsWhenCanceled()
        {
            var service = new InventoryModuleService();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                service.AddItemAsync(
                    new InventoryItemDelta(OwnerId, "wood", 1, _regularCategory),
                    cts.Token)
                    .GetAwaiter()
                    .GetResult());
        }

        private static TestItemCategory CreateCategory(string categoryId, string displayName)
        {
            var category = ScriptableObject.CreateInstance<TestItemCategory>();
            category.Initialize(categoryId, displayName);
            return category;
        }

        private sealed class TestItemCategory : ItemCategory
        {
            public void Initialize(string categoryId, string displayName)
            {
                SetIdentity(categoryId, displayName);
            }
        }
    }
}
