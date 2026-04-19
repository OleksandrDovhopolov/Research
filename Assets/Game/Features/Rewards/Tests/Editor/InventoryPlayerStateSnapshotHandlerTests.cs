using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using NUnit.Framework;
using UnityEngine;

namespace Rewards.Tests.Editor
{
    public sealed class InventoryPlayerStateSnapshotHandlerTests
    {
        [Test]
        public void ApplyAsync_MapsInventoryItemsAndAppliesSnapshot()
        {
            var snapshotService = new StubInventorySnapshotService();
            var handler = new InventoryPlayerStateSnapshotHandler(
                snapshotService,
                new StubPlayerIdentityProvider("player-1"),
                new StubInventoryItemCategoryResolver());

            handler.ApplyAsync(new PlayerStateSnapshotDto
            {
                InventoryItems = new List<InventoryItemDto>
                {
                    new() { ItemId = "pack_blue", Amount = 2 },
                    new() { ItemId = "unknown_item", Amount = 1 }
                }
            }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(snapshotService.LastSnapshot, Is.Not.Null);
            Assert.That(snapshotService.LastSnapshot.Items.Count, Is.EqualTo(2));
            Assert.That(snapshotService.LastSnapshot.Items[0].CategoryId, Is.EqualTo("card_pack"));
            Assert.That(snapshotService.LastSnapshot.Items[1].CategoryId, Is.EqualTo("regular"));
        }

        [Test]
        public void RewardSpecInventoryItemCategoryResolver_UsesRewardConfigAndFallback()
        {
            var config = ScriptableObject.CreateInstance<RewardSpecsConfigSO>();
            config.RewardSpecs = new List<RewardSpec>
            {
                new()
                {
                    RewardId = "reward_1",
                    Resources = new List<RewardSpecResource>
                    {
                        new() { ResourceId = "pack_blue", Kind = RewardKind.InventoryItem, Amount = 1, Category = "card_pack" }
                    }
                }
            };

            var resolver = new RewardSpecInventoryItemCategoryResolver(config);

            Assert.That(resolver.ResolveCategoryId("pack_blue"), Is.EqualTo("card_pack"));
            Assert.That(resolver.ResolveCategoryId("unknown_item"), Is.EqualTo("regular"));
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

        private sealed class StubInventoryItemCategoryResolver : IInventoryItemCategoryResolver
        {
            public string ResolveCategoryId(string itemId)
            {
                return itemId == "pack_blue" ? "card_pack" : "regular";
            }
        }

        private sealed class StubInventorySnapshotService : IInventorySnapshotService
        {
            public InventorySnapshotDto LastSnapshot { get; private set; }

            public UniTask ApplySnapshotAsync(InventorySnapshotDto snapshot, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LastSnapshot = snapshot;
                return UniTask.CompletedTask;
            }

            public UniTask ApplySnapshotAsync(IReadOnlyList<InventoryItemView> items, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }
    }
}
