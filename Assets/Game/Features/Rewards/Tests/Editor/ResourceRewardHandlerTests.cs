using System;
using System.Threading;
using CoreResources;
using NUnit.Framework;

namespace Rewards.Tests.Editor
{
    [TestFixture]
    public sealed class ResourceRewardHandlerTests
    {
        [Test]
        public void CanHandle_ReturnsTrueOnlyForResourceKind()
        {
            var handler = new ResourceRewardHandler(new ResourceManager(null));

            Assert.IsTrue(handler.CanHandle(new RewardGrantRequest("Gold", RewardKind.Resource, 5, "regular")));
            Assert.IsFalse(handler.CanHandle(new RewardGrantRequest("Pack", RewardKind.InventoryItem, 1, "card_pack")));
        }

        [Test]
        public void HandleAsync_ValidResource_UpdatesResourceManager()
        {
            var resourceManager = new ResourceManager(null);
            var handler = new ResourceRewardHandler(resourceManager);

            handler.HandleAsync(new RewardGrantRequest("Gold", RewardKind.Resource, 10, "regular"), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.AreEqual(10, resourceManager.Get(ResourceType.Gold));
        }

        [Test]
        public void HandleAsync_InvalidResourceId_ThrowsInvalidOperationException()
        {
            var handler = new ResourceRewardHandler(new ResourceManager(null));

            Assert.Throws<InvalidOperationException>(() =>
                handler.HandleAsync(new RewardGrantRequest("unknown_resource", RewardKind.Resource, 10, "regular"), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());
        }
    }
}
