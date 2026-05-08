using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Crafting;
using NUnit.Framework;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class CraftingServiceTests
    {
        [Test]
        public void StartCraftAsync_CreatesTaskAndBlocksOccupiedSlot()
        {
            var fixture = CreateFixture();

            var first = fixture.Service.StartCraftAsync("craft_green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            var second = fixture.Service.StartCraftAsync("craft_green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.False);
            Assert.That(second.Error, Is.EqualTo(CraftingError.StationQueueFull));
        }

        [Test]
        public void CollectAsync_BeforeComplete_ReturnsNotReady()
        {
            var fixture = CreateFixture();
            var start = fixture.Service.StartCraftAsync("craft_green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var collect = fixture.Service.CollectAsync(start.TaskId, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(collect.Success, Is.False);
            Assert.That(collect.Error, Is.EqualTo(CraftingError.TaskNotReady));
            Assert.That(fixture.Inventory.GetAmount("item_green_lure"), Is.EqualTo(0));
        }

        [Test]
        public void CollectAsync_AfterComplete_GrantsOutput()
        {
            var fixture = CreateFixture();
            var start = fixture.Service.StartCraftAsync("craft_green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddSeconds(10);

            var collect = fixture.Service.CollectAsync(start.TaskId, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(collect.Success, Is.True);
            Assert.That(collect.OutputItemId, Is.EqualTo("item_green_lure"));
            Assert.That(fixture.Inventory.GetAmount("item_green_lure"), Is.EqualTo(1));
        }

        private static Fixture CreateFixture()
        {
            var data = new CraftingStaticData(new List<CraftingRecipeConfig>
            {
                new()
                {
                    Id = "craft_green_lure",
                    DisplayName = "Craft Green Lure",
                    StationId = "lure_crafting_station",
                    OutputItemId = "item_green_lure",
                    OutputCount = 1,
                    CraftTimeSeconds = 5,
                    Ingredients = new List<CraftingIngredientConfig>(),
                    Requirements = new List<CraftingRequirementConfig>(),
                    IsEnabled = true
                }
            });
            var inventory = new FakeCraftingInventoryGateway();
            var clock = new FakeCraftingClock { UtcNow = DateTimeOffset.UtcNow };
            var service = new CraftingService(new FakeCraftingConfigProvider(data), inventory, clock);
            return new Fixture(service, inventory, clock);
        }

        private sealed class Fixture
        {
            public Fixture(CraftingService service, FakeCraftingInventoryGateway inventory, FakeCraftingClock clock)
            {
                Service = service;
                Inventory = inventory;
                Clock = clock;
            }

            public CraftingService Service { get; }
            public FakeCraftingInventoryGateway Inventory { get; }
            public FakeCraftingClock Clock { get; }
        }

        private sealed class FakeCraftingConfigProvider : ICraftingConfigProvider
        {
            private readonly CraftingStaticData _data;

            public FakeCraftingConfigProvider(CraftingStaticData data)
            {
                _data = data;
            }

            public UniTask<CraftingStaticData> LoadAsync(CancellationToken ct)
            {
                return UniTask.FromResult(_data);
            }

            public void ClearCache()
            {
            }
        }

        private sealed class FakeCraftingInventoryGateway : ICraftingInventoryGateway
        {
            private readonly Dictionary<string, int> _items = new();

            public int GetAmount(string itemId)
            {
                return _items.TryGetValue(itemId, out var amount) ? amount : 0;
            }

            public UniTask AddItemAsync(string itemId, int amount, CancellationToken ct = default)
            {
                _items[itemId] = GetAmount(itemId) + amount;
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeCraftingClock : ICraftingClock
        {
            public DateTimeOffset UtcNow { get; set; }
        }
    }
}
