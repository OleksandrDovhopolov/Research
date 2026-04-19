using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Inventory.Implementation.Services;
using NUnit.Framework;

namespace Inventory.Tests.Editor
{
    public sealed class InventoryCheatModuleTests
    {
        [Test]
        public void AddItemAsync_ThrowsNotSupportedException()
        {
            var service = CreateService(new StubInventoryServerApi());

            Assert.Throws<NotSupportedException>(() =>
                service.AddItemAsync(new InventoryItemDelta("ignored", "gold", 1, "regular"), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());
        }

        [Test]
        public void GetItemsAsync_LoadsSnapshot_FromServer()
        {
            var api = new StubInventoryServerApi
            {
                LoadResponse = new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto
                    {
                        Items = new List<InventorySnapshotItemDto>
                        {
                            new() { ItemId = "pack_a", Amount = 2, CategoryId = "card_pack" }
                        }
                    }
                }
            };
            var service = CreateService(api);

            var items = service.GetItemsAsync("legacy_owner", "card_pack", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0].OwnerId, Is.EqualTo("player-1"));
            Assert.That(items[0].ItemId, Is.EqualTo("pack_a"));
            Assert.That(items[0].StackCount, Is.EqualTo(2));
        }

        [Test]
        public void RemoveItemAsync_CallsServerAndAppliesSnapshot()
        {
            var api = new StubInventoryServerApi
            {
                LoadResponse = new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto
                    {
                        Items = new List<InventorySnapshotItemDto>
                        {
                            new() { ItemId = "pack_a", Amount = 3, CategoryId = "card_pack" }
                        }
                    }
                },
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
            var service = CreateService(api);

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
                LoadResponse = new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto
                    {
                        Items = new List<InventorySnapshotItemDto>
                        {
                            new() { ItemId = "pack_a", Amount = 2, CategoryId = "card_pack" },
                            new() { ItemId = "pack_b", Amount = 1, CategoryId = "card_pack" }
                        }
                    }
                },
                RemoveBatchResponse = new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto
                    {
                        Items = new List<InventorySnapshotItemDto>()
                    }
                }
            };
            var service = CreateService(api);

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

        private static InventoryModuleService CreateService(StubInventoryServerApi api)
        {
            return new InventoryModuleService(api, new StubPlayerIdentityProvider("player-1"));
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
            public InventoryOperationResponse LoadResponse { get; set; }
            public InventoryOperationResponse RemoveResponse { get; set; }
            public InventoryOperationResponse RemoveBatchResponse { get; set; }

            public RemoveInventoryItemCommand LastRemoveCommand { get; private set; }
            public RemoveInventoryBatchCommand LastRemoveBatchCommand { get; private set; }

            public UniTask<InventoryOperationResponse> LoadAsync(InventoryLoadCommand command, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(LoadResponse ?? new InventoryOperationResponse
                {
                    Success = true,
                    Inventory = new InventorySnapshotDto()
                });
            }

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
    }
}
