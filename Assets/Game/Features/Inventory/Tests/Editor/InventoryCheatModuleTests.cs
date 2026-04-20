using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Inventory.Implementation.Services;
using NUnit.Framework;
using Rewards;

namespace Inventory.Tests.Editor
{
    public sealed class InventoryCheatModuleTests
    {
        [Test]
        public void AddItemAsync_ThrowsNotSupportedException()
        {
            var service = CreateService(new StubInventoryServerApi(), "{}");

            Assert.Throws<NotSupportedException>(() =>
                service.AddItemAsync(new InventoryItemDelta("ignored", "gold", 1, "regular"), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());
        }

        [Test]
        public void GetItemsAsync_LoadsSnapshot_FromGlobalSaveInventoryItems()
        {
            var json = "{ \"Inventory\": { \"InventoryItems\": { \"pack_a\": 2 } } }";
            var service = CreateService(new StubInventoryServerApi(), json);

            var items = service.GetItemsAsync("legacy_owner", "card_pack", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].OwnerId, Is.EqualTo("player-1"));
            Assert.That(items[0].ItemId, Is.EqualTo("pack_a"));
            Assert.That(items[0].StackCount, Is.EqualTo(2));
        }

        [Test]
        public void GetItemsAsync_FallsBackToLegacyOwners_WhenInventoryItemsMissing()
        {
            var json =
                "{ \"Inventory\": { \"Owners\": [ { \"OwnerId\": \"player-1\", \"Items\": [ { \"OwnerId\": \"player-1\", \"ItemId\": \"legacy_pack\", \"StackCount\": 3, \"CategoryId\": \"card_pack\" } ] } ] } }";
            var service = CreateService(new StubInventoryServerApi(), json);

            var items = service.GetItemsAsync("legacy_owner", "card_pack", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].ItemId, Is.EqualTo("legacy_pack"));
            Assert.That(items[0].StackCount, Is.EqualTo(3));
            Assert.That(items[0].CategoryId, Is.EqualTo("card_pack"));
        }

        [Test]
        public void RemoveItemAsync_CallsServerAndAppliesSnapshot()
        {
            var api = new StubInventoryServerApi
            {
                RemoveResponse = new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto
                    {
                        Items = new List<InventorySnapshotItemDto>
                        {
                            new() { ItemId = "pack_a", Amount = 1, CategoryId = "card_pack" }
                        }
                    }
                }
            };
            var service = CreateService(api, "{ \"Inventory\": { \"InventoryItems\": { \"pack_a\": 3 } } }");

            service.RemoveItemAsync(new InventoryItemDelta("legacy", "pack_a", 2, "card_pack"), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(api.LastRemoveCommand, Is.Not.Null);
            Assert.That(api.LastRemoveCommand.PlayerId, Is.EqualTo("player-1"));
            Assert.That(api.LastRemoveCommand.ItemId, Is.EqualTo("pack_a"));
            Assert.That(api.LastRemoveCommand.Amount, Is.EqualTo(2));

            var remaining = service.GetItemsAsync("legacy", "card_pack", CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            Assert.That(remaining.Count, Is.EqualTo(1));
            Assert.That(remaining[0].StackCount, Is.EqualTo(1));
        }

        [Test]
        public void RemoveItemsAsync_UsesBatchEndpoint_AndReturnsRequestedAsRemoved()
        {
            var api = new StubInventoryServerApi
            {
                RemoveBatchResponse = new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto
                    {
                        Items = new List<InventorySnapshotItemDto>()
                    }
                }
            };
            var service = CreateService(api, "{ \"Inventory\": { \"InventoryItems\": { \"pack_a\": 2, \"pack_b\": 1 } } }");

            var result = service.RemoveItemsAsync(new List<InventoryItemDelta>
            {
                new("legacy", "pack_a", 2, "card_pack"),
                new("legacy", "pack_b", 1, "card_pack")
            }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(api.LastRemoveBatchCommand, Is.Not.Null);
            Assert.That(api.LastRemoveBatchCommand.PlayerId, Is.EqualTo("player-1"));
            Assert.That(api.LastRemoveBatchCommand.Items.Count, Is.EqualTo(2));
            Assert.That(result.RequestedStacks, Is.EqualTo(3));
            Assert.That(result.RemovedStacks, Is.EqualTo(3));
        }

        [Test]
        public void GetItemsAsync_UsesRegularFallbackCategory_ForUnknownItem()
        {
            var json = "{ \"Inventory\": { \"InventoryItems\": { \"unknown_box\": 1 } } }";
            var service = CreateService(new StubInventoryServerApi(), json);

            var items = service.GetItemsAsync("legacy_owner", InventoryBuiltInCategoryIds.Regular, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].CategoryId, Is.EqualTo(InventoryBuiltInCategoryIds.Regular));
        }

        private static InventoryModuleService CreateService(StubInventoryServerApi api, string saveJson)
        {
            var storage = new InMemorySaveStorage(saveJson);
            var saveService = new SaveService(storage, new SaveMigrationService());
            var resolver = new StubInventoryItemCategoryResolver(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["pack_a"] = InventoryBuiltInCategoryIds.CardPack,
                ["pack_b"] = InventoryBuiltInCategoryIds.CardPack,
                ["legacy_pack"] = InventoryBuiltInCategoryIds.CardPack
            });

            return new InventoryModuleService(
                api,
                new StubPlayerIdentityProvider("player-1"),
                saveService,
                resolver);
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

        private sealed class StubInventoryServerApi : IInventoryServerApi
        {
            public InventoryOperationResponse RemoveResponse { get; set; }
            public InventoryOperationResponse RemoveBatchResponse { get; set; }

            public RemoveInventoryItemCommand LastRemoveCommand { get; private set; }
            public RemoveInventoryBatchCommand LastRemoveBatchCommand { get; private set; }

            public UniTask<InventoryOperationResponse> RemoveAsync(RemoveInventoryItemCommand command, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LastRemoveCommand = command;
                return UniTask.FromResult(RemoveResponse ?? new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto()
                });
            }

            public UniTask<InventoryOperationResponse> RemoveBatchAsync(RemoveInventoryBatchCommand command, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LastRemoveBatchCommand = command;
                return UniTask.FromResult(RemoveBatchResponse ?? new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto()
                });
            }
        }

        private sealed class InMemorySaveStorage : ISaveStorage
        {
            private string _json;

            public InMemorySaveStorage(string json)
            {
                _json = json ?? "{}";
            }

            public UniTask SaveAsync(string data, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _json = data ?? "{}";
                return UniTask.CompletedTask;
            }

            public UniTask<string> LoadAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(_json);
            }

            public bool Exists()
            {
                return true;
            }

            public UniTask DeleteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _json = "{}";
                return UniTask.CompletedTask;
            }

            public UniTask<long> GetLastModifiedTimestampAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(0L);
            }
        }

        private sealed class StubInventoryItemCategoryResolver : IInventoryItemCategoryResolver
        {
            private readonly IReadOnlyDictionary<string, string> _categoryByItemId;

            public StubInventoryItemCategoryResolver(IReadOnlyDictionary<string, string> categoryByItemId)
            {
                _categoryByItemId = categoryByItemId;
            }

            public string ResolveCategoryId(string itemId)
            {
                if (!string.IsNullOrWhiteSpace(itemId) &&
                    _categoryByItemId.TryGetValue(itemId, out var categoryId) &&
                    !string.IsNullOrWhiteSpace(categoryId))
                {
                    return categoryId;
                }

                return InventoryBuiltInCategoryIds.Regular;
            }
        }
    }
}
