using System;
using System.Threading;
using System.Threading.Tasks;
using Inventory.API;
using Inventory.Implementation.Services;
using NUnit.Framework;

namespace Inventory.Tests.Editor
{
    public sealed class InventoryModuleTests
    {
        private const string OwnerId = "player_1";

        [Test]
        public async Task AddItem_StacksByOwnerItemAndCategory()
        {
            var service = new InventoryModuleService();

            await service.AddItemAsync(new InventoryItemDelta(OwnerId, "wood", "Wood", 2, InventoryItemCategory.Regular));
            await service.AddItemAsync(new InventoryItemDelta(OwnerId, "wood", "Wood", 3, InventoryItemCategory.Regular));

            var items = await service.GetItemsAsync(OwnerId, InventoryItemCategory.Regular);

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].StackCount, Is.EqualTo(5));
        }

        [Test]
        public async Task RemoveItem_DecrementsAndDeletesEntityAtZero()
        {
            var service = new InventoryModuleService();

            await service.AddItemAsync(new InventoryItemDelta(OwnerId, "energy", "Energy", 5, InventoryItemCategory.Regular));
            await service.RemoveItemAsync(new InventoryItemDelta(OwnerId, "energy", "Energy", 2, InventoryItemCategory.Regular));

            var afterPartialRemove = await service.GetItemsAsync(OwnerId, InventoryItemCategory.Regular);
            Assert.That(afterPartialRemove.Count, Is.EqualTo(1));
            Assert.That(afterPartialRemove[0].StackCount, Is.EqualTo(3));

            await service.RemoveItemAsync(new InventoryItemDelta(OwnerId, "energy", "Energy", 3, InventoryItemCategory.Regular));

            var afterFullRemove = await service.GetItemsAsync(OwnerId, InventoryItemCategory.Regular);
            Assert.That(afterFullRemove.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task QuerySystem_FiltersByCategory_AndPreservesCardPackMetadata()
        {
            var service = new InventoryModuleService();

            await service.AddItemAsync(new InventoryItemDelta(OwnerId, "gold", "Gold", 10, InventoryItemCategory.Regular));
            await service.AddItemAsync(new InventoryItemDelta(
                OwnerId,
                "pack_blue",
                "CardPack",
                1,
                InventoryItemCategory.CardPack,
                new CardPackMetadata("Blue Pack", 5)));

            var regular = await service.GetItemsAsync(OwnerId, InventoryItemCategory.Regular);
            var packs = await service.GetItemsAsync(OwnerId, InventoryItemCategory.CardPack);

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

            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await service.AddItemAsync(
                    new InventoryItemDelta(OwnerId, "wood", "Wood", 1, InventoryItemCategory.Regular),
                    cts.Token));
        }
    }
}
