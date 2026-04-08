using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Inventory.API;
using Inventory.Implementation;
using Inventory.Implementation.Services;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Inventory.Tests.Editor
{
    public sealed class InventoryCheatModuleTests
    {
        private const string OwnerId = "player_1";
        private TestItemCategory _regularCategory;
        private TestItemCategory _cardPackCategory;
        private TestItemCategory _alchemyCategory;

        [SetUp]
        public void SetUp()
        {
            _regularCategory = new TestItemCategory(InventoryBuiltInCategoryIds.Regular, "Regular");
            _cardPackCategory = new TestItemCategory(InventoryBuiltInCategoryIds.CardPack, "Card packs");
            _alchemyCategory = new TestItemCategory("alchemy", "Alchemy");
        }

        [UnityTest]
        public IEnumerator AddItem_StacksByOwnerItemAndCategory()
        {
            var service = CreateService();

            yield return service.AddItemAsync(new InventoryItemDelta(OwnerId, "Gems", 2, _regularCategory))
                .AsTask()
                .ToCoroutine();

            yield return service.AddItemAsync(new InventoryItemDelta(OwnerId, "Gems", 3, _regularCategory))
                .AsTask()
                .ToCoroutine();

            IReadOnlyList<InventoryItemView> items = null;
            yield return service.GetItemsAsync(OwnerId, _regularCategory.CategoryId)
                .ToCoroutine(result => items = result);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].StackCount, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator RemoveItem_DecrementsAndDeletesEntityAtZero()
        {
            var service = CreateService();

            yield return service.AddItemAsync(new InventoryItemDelta(OwnerId, "Energy", 5, _regularCategory))
                .AsTask()
                .ToCoroutine();
            yield return service.RemoveItemAsync(new InventoryItemDelta(OwnerId, "Energy", 2, _regularCategory))
                .AsTask()
                .ToCoroutine();

            IReadOnlyList<InventoryItemView> afterPartialRemove = null;
            yield return service.GetItemsAsync(OwnerId, _regularCategory.CategoryId)
                .ToCoroutine(result => afterPartialRemove = result);
            Assert.That(afterPartialRemove.Count, Is.EqualTo(1));
            Assert.That(afterPartialRemove[0].StackCount, Is.EqualTo(3));

            yield return service.RemoveItemAsync(new InventoryItemDelta(OwnerId, "Energy", 3, _regularCategory))
                .AsTask()
                .ToCoroutine();

            IReadOnlyList<InventoryItemView> afterFullRemove = null;
            yield return service.GetItemsAsync(OwnerId, _regularCategory.CategoryId)
                .ToCoroutine(result => afterFullRemove = result);
            Assert.That(afterFullRemove.Count, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator QuerySystem_FiltersByCategory_AndPreservesCardPackMetadata()
        {
            var service = CreateService();

            yield return service.AddItemAsync(new InventoryItemDelta(OwnerId, "Gold", 10, _regularCategory))
                .AsTask()
                .ToCoroutine();
            
            yield return service.AddItemAsync(new InventoryItemDelta(
                    OwnerId,
                    "pack_blue",
                    1,
                    _cardPackCategory))
                .AsTask()
                .ToCoroutine();

            IReadOnlyList<InventoryItemView> regular = null;
            IReadOnlyList<InventoryItemView> packs = null;
            yield return service.GetItemsAsync(OwnerId, _regularCategory.CategoryId)
                .ToCoroutine(result => regular = result);
            yield return service.GetItemsAsync(OwnerId, _cardPackCategory.CategoryId)
                .ToCoroutine(result => packs = result);

            Assert.That(regular.Count, Is.EqualTo(1));
            Assert.That(regular[0].ItemId, Is.EqualTo("Gold"));

            Assert.That(packs.Count, Is.EqualTo(1));
            Assert.That(packs[0].ItemId, Is.EqualTo("pack_blue"));
        }

        [UnityTest]
        public IEnumerator AddItem_AllowsNewCategoryWithoutCoreChanges()
        {
            var service = CreateService();

            yield return service.AddItemAsync(new InventoryItemDelta(OwnerId, "Dust", 4, _alchemyCategory))
                .AsTask()
                .ToCoroutine();

            IReadOnlyList<InventoryItemView> alchemyItems = null;
            yield return service.GetItemsAsync(OwnerId, _alchemyCategory.CategoryId)
                .ToCoroutine(result => alchemyItems = result);

            Assert.That(alchemyItems.Count, Is.EqualTo(1));
            Assert.That(alchemyItems[0].ItemId, Is.EqualTo("Dust"));
            Assert.That(alchemyItems[0].StackCount, Is.EqualTo(4));
            Assert.That(alchemyItems[0].CategoryId, Is.EqualTo(_alchemyCategory.CategoryId));
        }

        [UnityTest]
        public IEnumerator AddItemAsync_ThrowsWhenCanceled()
        {
            var service = CreateService();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.That(() => 
            {
                service.AddItemAsync(new InventoryItemDelta(OwnerId, "Gems", 1, _regularCategory), cts.Token)
                    .GetAwaiter()
                    .GetResult();
            }, Throws.TypeOf<OperationCanceledException>());
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveItemsAsync_RemovesAvailableStacksAndReturnsFailures()
        {
            var service = CreateService();

            yield return service.AddItemAsync(new InventoryItemDelta(OwnerId, "pack_blue", 3, _cardPackCategory))
                .AsTask()
                .ToCoroutine();
            yield return service.AddItemAsync(new InventoryItemDelta(OwnerId, "pack_red", 1, _cardPackCategory))
                .AsTask()
                .ToCoroutine();

            InventoryBatchRemoveResult removeResult = default;
            var removeRequest = new List<InventoryItemDelta>
            {
                new(OwnerId, "pack_blue", 2, _cardPackCategory),
                new(OwnerId, "pack_red", 1, _cardPackCategory),
                new(OwnerId, "pack_missing", 1, _cardPackCategory),
            };

            yield return service.RemoveItemsAsync(removeRequest, CancellationToken.None)
                .ToCoroutine(result => removeResult = result);

            Assert.That(removeResult.RequestedStacks, Is.EqualTo(4));
            Assert.That(removeResult.RemovedStacks, Is.EqualTo(3));
            Assert.That(removeResult.FailedItems.Count, Is.EqualTo(1));
            Assert.That(removeResult.FailedItems[0].ItemId, Is.EqualTo("pack_missing"));

            IReadOnlyList<InventoryItemView> packs = null;
            yield return service.GetItemsAsync(OwnerId, _cardPackCategory.CategoryId, CancellationToken.None)
                .ToCoroutine(result => packs = result);

            Assert.That(packs.Count, Is.EqualTo(1));
            Assert.That(packs[0].ItemId, Is.EqualTo("pack_blue"));
            Assert.That(packs[0].StackCount, Is.EqualTo(1));
        }

        private static InventoryModuleService CreateService()
        {
            return new InventoryModuleService(new TestInventoryStorage());
        }

        private sealed class TestItemCategory : ItemCategory
        {
            public TestItemCategory(string categoryId, string displayName)
                : base(categoryId, displayName)
            {
            }

            public override CategoryUiMetadata GetMetadata()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class TestInventoryStorage : IInventoryStorage
        {
            private readonly Dictionary<string, IReadOnlyList<InventoryItemView>> _storage = new();

            public UniTask<IReadOnlyList<InventoryItemView>> LoadAsync(string ownerId, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_storage.TryGetValue(ownerId, out var items))
                {
                    return UniTask.FromResult(items);
                }

                return UniTask.FromResult((IReadOnlyList<InventoryItemView>)Array.Empty<InventoryItemView>());
            }

            public UniTask SaveAsync(string ownerId, IReadOnlyList<InventoryItemView> items, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _storage[ownerId] = items == null ? Array.Empty<InventoryItemView>() : new List<InventoryItemView>(items);
                return UniTask.CompletedTask;
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

        public static IEnumerator ToCoroutine<T>(this UniTask<T> task, Action<T> onResult)
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
