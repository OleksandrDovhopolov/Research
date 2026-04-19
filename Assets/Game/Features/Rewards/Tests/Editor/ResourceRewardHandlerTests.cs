using System;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Rewards.Tests.Editor
{
    [TestFixture]
    public sealed class ResourceRewardHandlerTests
    {
        [Test]
        public void CanHandle_ReturnsTrueOnlyForResourceKind()
        {
            var handler = new ResourceRewardHandler(new StubResourceOperationsService());

            Assert.IsTrue(handler.CanHandle(new RewardGrantRequest("Gold", RewardKind.Resource, 5, "regular")));
            Assert.IsFalse(handler.CanHandle(new RewardGrantRequest("Pack", RewardKind.InventoryItem, 1, "card_pack")));
        }

        [Test]
        public void HandleAsync_ValidResource_UpdatesResourceManager()
        {
            var resourceManager = CreateResourceManager();
            var operationsService = new StubResourceOperationsService();
            operationsService.AddHandler = (type, amount, _, ct) =>
            {
                var snapshot = type switch
                {
                    ResourceType.Gold => new ResourceSnapshotDto { Gold = Math.Max(0, amount) },
                    ResourceType.Energy => new ResourceSnapshotDto { Energy = Math.Max(0, amount) },
                    ResourceType.Gems => new ResourceSnapshotDto { Gems = Math.Max(0, amount) },
                    _ => new ResourceSnapshotDto()
                };

                return resourceManager.ApplySnapshotAsync(snapshot, ct);
            };
            var handler = new ResourceRewardHandler(operationsService);

            handler.HandleAsync(new RewardGrantRequest("Gold", RewardKind.Resource, 10, "regular"), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual(10, resourceManager.Get(ResourceType.Gold));
        }

        [Test]
        public void HandleAsync_InvalidResourceId_ThrowsInvalidOperationException()
        {
            var handler = new ResourceRewardHandler(new StubResourceOperationsService());

            Assert.Throws<InvalidOperationException>(() =>
                handler.HandleAsync(new RewardGrantRequest("unknown_resource", RewardKind.Resource, 10, "regular"), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());
        }

        private static ResourceManager CreateResourceManager()
        {
            return new ResourceManager(null);
        }

        private sealed class StubResourceOperationsService : IResourceOperationsService
        {
            public Func<ResourceType, int, string, CancellationToken, UniTask> AddHandler { get; set; } =
                (_, _, _, _) => UniTask.CompletedTask;

            public UniTask AddAsync(
                ResourceType type,
                int amount,
                string reason = ResourceManager.RewardGrantReason,
                CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return AddHandler(type, amount, reason, ct);
            }

            public UniTask<bool> RemoveAsync(
                ResourceType type,
                int amount,
                string reason = ResourceManager.CheatRemoveReason,
                CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(true);
            }
        }
    }
}
