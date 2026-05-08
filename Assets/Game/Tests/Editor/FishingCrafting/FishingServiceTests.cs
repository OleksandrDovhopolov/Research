using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using NUnit.Framework;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class FishingServiceTests
    {
        [Test]
        public void StartFishingAsync_RemovesLureOnStart()
        {
            var fixture = CreateFixture();
            fixture.Inventory.Add("item_green_lure", 1);

            var result = fixture.Service.StartFishingAsync("zone_1", "green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result.Success, Is.True);
            Assert.That(fixture.Inventory.GetAmount("item_green_lure"), Is.EqualTo(0));
        }

        [Test]
        public void CompleteFishingAsync_WhenMinigameFails_DoesNotGrantFish()
        {
            var fixture = CreateFixture();
            fixture.Inventory.Add("item_green_lure", 1);
            var start = fixture.Service.StartFishingAsync("zone_1", "green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var result = fixture.Service.CompleteFishingAsync(start.AttemptId, false, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(FishingError.MinigameFailed));
            Assert.That(fixture.Inventory.GetAmount("item_roach"), Is.EqualTo(0));
            Assert.That(fixture.Book.Registered.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompleteFishingAsync_WhenMinigameSucceeds_GrantsFishAndUpdatesBook()
        {
            var fixture = CreateFixture();
            fixture.Inventory.Add("item_green_lure", 1);
            var start = fixture.Service.StartFishingAsync("zone_1", "green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var result = fixture.Service.CompleteFishingAsync(start.AttemptId, true, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(result.Success, Is.True);
            Assert.That(result.FishId, Is.EqualTo("roach"));
            Assert.That(fixture.Inventory.GetAmount("item_roach"), Is.EqualTo(1));
            Assert.That(fixture.Book.Registered.Count, Is.EqualTo(1));
        }

        private static Fixture CreateFixture()
        {
            var data = new FishingStaticData(
                new List<FishConfig>
                {
                    new()
                    {
                        Id = "roach",
                        DisplayName = "Roach",
                        AvailableLureIds = new List<string> { "green_lure" },
                        WaterBodyTypes = new List<string> { "coastal" },
                        SpawnWeight = 1,
                        WeightThresholds = new FishWeightThresholds
                        {
                            Common = 1,
                            Rare = 2,
                            Epic = 3,
                            Legendary = 4
                        }
                    }
                },
                new List<FishingZoneConfig>
                {
                    new()
                    {
                        Id = "zone_1",
                        DisplayName = "Zone 1",
                        WaterBodyType = "coastal",
                        IsUnlockedByDefault = true,
                        AllowedLureIds = new List<string> { "green_lure" }
                    }
                },
                new List<WaterBodyTypeConfig> { new() { Id = "coastal", DisplayName = "Coastal" } },
                new List<LureConfig>
                {
                    new()
                    {
                        Id = "green_lure",
                        ItemId = "item_green_lure",
                        CraftRecipeId = "craft_green_lure"
                    }
                },
                new List<FishingItemConfig>
                {
                    new() { Id = "item_green_lure", Type = "lure", Stackable = true, MaxStack = 999 },
                    new() { Id = "item_roach", Type = "fish", Stackable = true, MaxStack = 999 }
                },
                new FishingSettingsConfigRoot(),
                new[] { "craft_green_lure" });

            var inventory = new FakeFishingInventoryGateway();
            var book = new FakeFishBookService();
            var service = new FishingService(
                new FakeFishingConfigProvider(data),
                new FishSelector(new FixedFishingRandom(0)),
                new FishWeightService(new FixedFishingRandom(0.5)),
                book,
                inventory,
                new EmptyActiveFishingEventsProvider());

            return new Fixture(service, inventory, book);
        }

        private sealed class Fixture
        {
            public Fixture(FishingService service, FakeFishingInventoryGateway inventory, FakeFishBookService book)
            {
                Service = service;
                Inventory = inventory;
                Book = book;
            }

            public FishingService Service { get; }
            public FakeFishingInventoryGateway Inventory { get; }
            public FakeFishBookService Book { get; }
        }

        private sealed class FakeFishingConfigProvider : IFishingConfigProvider
        {
            private readonly FishingStaticData _data;

            public FakeFishingConfigProvider(FishingStaticData data)
            {
                _data = data;
            }

            public UniTask<FishingStaticData> LoadAsync(CancellationToken ct)
            {
                return UniTask.FromResult(_data);
            }

            public void ClearCache()
            {
            }
        }

        private sealed class FakeFishingInventoryGateway : IFishingInventoryGateway
        {
            private readonly Dictionary<string, int> _items = new();

            public void Add(string itemId, int amount)
            {
                _items[itemId] = GetAmount(itemId) + amount;
            }

            public int GetAmount(string itemId)
            {
                return _items.TryGetValue(itemId, out var amount) ? amount : 0;
            }

            public UniTask<bool> HasItemAsync(string itemId, int amount, CancellationToken ct = default)
            {
                return UniTask.FromResult(GetAmount(itemId) >= amount);
            }

            public UniTask<bool> RemoveItemAsync(string itemId, int amount, CancellationToken ct = default)
            {
                if (GetAmount(itemId) < amount)
                    return UniTask.FromResult(false);

                _items[itemId] = GetAmount(itemId) - amount;
                return UniTask.FromResult(true);
            }

            public UniTask AddItemAsync(string itemId, int amount, CancellationToken ct = default)
            {
                Add(itemId, amount);
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeFishBookService : IFishBookService
        {
            public List<FishingCatchResult> Registered { get; } = new();

            public UniTask RegisterCatchAsync(FishingCatchResult result, CancellationToken ct = default)
            {
                Registered.Add(result);
                return UniTask.CompletedTask;
            }

            public UniTask<FishBookProgress> GetProgressAsync(string fishId, CancellationToken ct = default)
            {
                return UniTask.FromResult<FishBookProgress>(null);
            }

            public UniTask MarkAsViewedAsync(string fishId, CancellationToken ct = default)
            {
                return UniTask.CompletedTask;
            }
        }

        private sealed class FixedFishingRandom : IFishingRandom
        {
            private readonly double _value;

            public FixedFishingRandom(double value)
            {
                _value = value;
            }

            public double NextDouble()
            {
                return _value;
            }
        }
    }
}
