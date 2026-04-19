using System;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;
using Infrastructure;
using NUnit.Framework;

namespace Rewards.Tests.Editor
{
    [TestFixture]
    public sealed class ResourceRewardHandlerTests
    {
        [Test]
        public void CanHandle_ReturnsTrueOnlyForResourceKind()
        {
            var handler = new ResourceRewardHandler(CreateResourceManager());

            Assert.IsTrue(handler.CanHandle(new RewardGrantRequest("Gold", RewardKind.Resource, 5, "regular")));
            Assert.IsFalse(handler.CanHandle(new RewardGrantRequest("Pack", RewardKind.InventoryItem, 1, "card_pack")));
        }

        [Test]
        public void HandleAsync_ValidResource_UpdatesResourceManager()
        {
            var resourceManager = CreateResourceManager();
            var handler = new ResourceRewardHandler(resourceManager);

            handler.HandleAsync(new RewardGrantRequest("Gold", RewardKind.Resource, 10, "regular"), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual(10, resourceManager.Get(ResourceType.Gold));
        }

        [Test]
        public void HandleAsync_InvalidResourceId_ThrowsInvalidOperationException()
        {
            var handler = new ResourceRewardHandler(CreateResourceManager());

            Assert.Throws<InvalidOperationException>(() =>
                handler.HandleAsync(new RewardGrantRequest("unknown_resource", RewardKind.Resource, 10, "regular"), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());
        }

        private static ResourceManager CreateResourceManager()
        {
            var api = new StubResourceAdjustApi(command => UniTask.FromResult(new AdjustResourceResponse
            {
                Success = true,
                Resources = command.ResourceId switch
                {
                    "Gold" => new ResourceSnapshotDto { Gold = Math.Max(0, command.Delta), Energy = 0, Gems = 0 },
                    "Energy" => new ResourceSnapshotDto { Gold = 0, Energy = Math.Max(0, command.Delta), Gems = 0 },
                    "Gems" => new ResourceSnapshotDto { Gold = 0, Energy = 0, Gems = Math.Max(0, command.Delta) },
                    _ => new ResourceSnapshotDto()
                }
            }));
            return new ResourceManager(null, new StubPlayerIdentityProvider(), api);
        }

        private sealed class StubPlayerIdentityProvider : IPlayerIdentityProvider
        {
            public string GetPlayerId()
            {
                return "player-test";
            }
        }

        private sealed class StubResourceAdjustApi : IResourceAdjustApi
        {
            private readonly Func<AdjustResourceCommand, UniTask<AdjustResourceResponse>> _handler;

            public StubResourceAdjustApi(Func<AdjustResourceCommand, UniTask<AdjustResourceResponse>> handler)
            {
                _handler = handler;
            }

            public UniTask<AdjustResourceResponse> AdjustAsync(AdjustResourceCommand command, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return _handler(command);
            }
        }
    }
}
