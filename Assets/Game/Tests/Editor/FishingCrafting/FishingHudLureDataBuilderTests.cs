using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using Infrastructure;
using Inventory.API;
using NUnit.Framework;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class FishingHudLureDataBuilderTests
    {
        [Test]
        public void BuildAsync_ReturnsSortedLuresWithCountsAndSpriteAddresses()
        {
            var inventoryReadService = new StubInventoryReadService(new[]
            {
                new InventoryItemView("player", "item_green_lure", 3, "regular"),
                new InventoryItemView("player", "item_green_lure", 2, "regular")
            });
            var builder = new FishingHudLureDataBuilder(
                new StubFishingConfigProvider(CreateStaticData()),
                inventoryReadService,
                new StubPlayerIdentityProvider("player"));

            var entries = builder.BuildAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(entries.Select(entry => entry.LureId), Is.EqualTo(new[] { "red_lure", "green_lure", "blue_lure" }));
            Assert.That(entries.Select(entry => entry.SpriteAddress), Is.EqualTo(new[] { "red_lure", "green_lure", "blue_lure" }));
            Assert.That(entries[0].Count, Is.EqualTo(0));
            Assert.That(entries[1].Count, Is.EqualTo(5));
            Assert.That(entries[2].Count, Is.EqualTo(0));
            Assert.That(inventoryReadService.OwnerId, Is.EqualTo("player"));
            Assert.That(inventoryReadService.CategoryId, Is.EqualTo("regular"));
        }

        private static FishingStaticData CreateStaticData()
        {
            return new FishingStaticData(
                Array.Empty<FishConfig>(),
                Array.Empty<FishingZoneConfig>(),
                Array.Empty<WaterBodyTypeConfig>(),
                new List<LureConfig>
                {
                    new()
                    {
                        Id = "green_lure",
                        DisplayName = "Green Lure",
                        ItemId = "item_green_lure",
                        CraftRecipeId = "craft_green_lure",
                        SortOrder = 2
                    },
                    new()
                    {
                        Id = "blue_lure",
                        DisplayName = "Blue Lure",
                        ItemId = "item_blue_lure",
                        CraftRecipeId = "craft_blue_lure",
                        SortOrder = 3
                    },
                    new()
                    {
                        Id = "red_lure",
                        DisplayName = "Red Lure",
                        ItemId = "item_red_lure",
                        CraftRecipeId = "craft_red_lure",
                        SortOrder = 1
                    }
                },
                Array.Empty<FishingItemConfig>(),
                new FishingSettingsConfigRoot(),
                Array.Empty<string>());
        }

        private sealed class StubFishingConfigProvider : IFishingConfigProvider
        {
            private readonly FishingStaticData _data;

            public StubFishingConfigProvider(FishingStaticData data)
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

        private sealed class StubInventoryReadService : IInventoryReadService
        {
            private readonly IReadOnlyList<InventoryItemView> _items;

            public StubInventoryReadService(IReadOnlyList<InventoryItemView> items)
            {
                _items = items;
            }

            public string OwnerId { get; private set; }
            public string CategoryId { get; private set; }

            public UniTask<IReadOnlyList<InventoryItemView>> GetItemsAsync(
                string ownerId,
                string categoryId,
                CancellationToken cancellationToken = default)
            {
                OwnerId = ownerId;
                CategoryId = categoryId;
                return UniTask.FromResult(_items);
            }
        }

        private sealed class StubPlayerIdentityProvider : IPlayerIdentityProvider
        {
            private readonly string _playerId;

            public StubPlayerIdentityProvider(string playerId)
            {
                _playerId = playerId;
            }

            public string GetPlayerId()
            {
                return _playerId;
            }
        }
    }
}
