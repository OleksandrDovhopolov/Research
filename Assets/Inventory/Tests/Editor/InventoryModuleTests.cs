using System;
using System.Threading;
using Inventory.API;
using Inventory.Implementation.Services;
using NUnit.Framework;

namespace Inventory.Tests.Editor
{
    public sealed class InventoryModuleTests
    {
        private const string OwnerId = "player_1";

        [Test]
        public void AddItem_StacksByOwnerItemAndCategory()
        {
            var service = new InventoryModuleService();

            service.AddItemAsync(new InventoryItemDelta(OwnerId, "wood", "Wood", 2, InventoryItemCategory.Regular))
                .GetAwaiter()
                .GetResult();
            service.AddItemAsync(new InventoryItemDelta(OwnerId, "wood", "Wood", 3, InventoryItemCategory.Regular))
                .GetAwaiter()
                .GetResult();

            var items = service.GetItemsAsync(OwnerId, InventoryItemCategory.Regular)
                .GetAwaiter()
                .GetResult();

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].StackCount, Is.EqualTo(5));
        }

        [Test]
        public void RemoveItem_DecrementsAndDeletesEntityAtZero()
        {
            var service = new InventoryModuleService();

            service.AddItemAsync(new InventoryItemDelta(OwnerId, "energy", "Energy", 5, InventoryItemCategory.Regular))
                .GetAwaiter()
                .GetResult();
            service.RemoveItemAsync(new InventoryItemDelta(OwnerId, "energy", "Energy", 2, InventoryItemCategory.Regular))
                .GetAwaiter()
                .GetResult();

            var afterPartialRemove = service.GetItemsAsync(OwnerId, InventoryItemCategory.Regular)
                .GetAwaiter()
                .GetResult();
            Assert.That(afterPartialRemove.Count, Is.EqualTo(1));
            Assert.That(afterPartialRemove[0].StackCount, Is.EqualTo(3));

            service.RemoveItemAsync(new InventoryItemDelta(OwnerId, "energy", "Energy", 3, InventoryItemCategory.Regular))
                .GetAwaiter()
                .GetResult();

            var afterFullRemove = service.GetItemsAsync(OwnerId, InventoryItemCategory.Regular)
                .GetAwaiter()
                .GetResult();
            Assert.That(afterFullRemove.Count, Is.EqualTo(0));
        }

        [Test]
        public void QuerySystem_FiltersByCategory_AndPreservesCardPackMetadata()
        {
            var service = new InventoryModuleService();

            service.AddItemAsync(new InventoryItemDelta(OwnerId, "gold", "Gold", 10, InventoryItemCategory.Regular))
                .GetAwaiter()
                .GetResult();
            service.AddItemAsync(new InventoryItemDelta(
                    OwnerId,
                    "pack_blue",
                    "CardPack",
                    1,
                    InventoryItemCategory.CardPack,
                    new CardPackMetadata("Blue Pack", 5)))
                .GetAwaiter()
                .GetResult();

            var regular = service.GetItemsAsync(OwnerId, InventoryItemCategory.Regular)
                .GetAwaiter()
                .GetResult();
            var packs = service.GetItemsAsync(OwnerId, InventoryItemCategory.CardPack)
                .GetAwaiter()
                .GetResult();

            Assert.That(regular.Count, Is.EqualTo(1));
            Assert.That(regular[0].ItemId, Is.EqualTo("gold"));
            Assert.That(regular[0].CardPackMetadata.HasValue, Is.False);

            Assert.That(packs.Count, Is.EqualTo(1));
            Assert.That(packs[0].ItemId, Is.EqualTo("pack_blue"));
            Assert.That(packs[0].CardPackMetadata.HasValue, Is.True);
            Assert.That(packs[0].CardPackMetadata.Value.PackName, Is.EqualTo("Blue Pack"));
            Assert.That(packs[0].CardPackMetadata.Value.CardsInside, Is.EqualTo(5));
        }

        [Test]
        public void AddItemAsync_ThrowsWhenCanceled()
        {
            var service = new InventoryModuleService();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                service.AddItemAsync(
                    new InventoryItemDelta(OwnerId, "wood", "Wood", 1, InventoryItemCategory.Regular),
                    cts.Token)
                    .GetAwaiter()
                    .GetResult());
        }
    }
}
