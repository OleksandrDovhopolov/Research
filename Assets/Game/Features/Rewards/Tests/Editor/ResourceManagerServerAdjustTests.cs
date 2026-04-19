using System;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;
using Infrastructure;
using NUnit.Framework;

namespace Rewards.Tests.Editor
{
    [TestFixture]
    public sealed class ResourceManagerServerAdjustTests
    {
        [Test]
        public void InitializeAsync_LoadsResourcesFromSave()
        {
            var fixture = CreateFixture(_ => UniTask.FromResult(new AdjustResourceResponse
            {
                Success = true,
                Resources = new ResourceSnapshotDto()
            }));

            fixture.SaveService
                .UpdateModuleAsync(data => data.Resources, resources =>
                {
                    resources.Gold = 11;
                    resources.Energy = 12;
                    resources.Gems = 13;
                }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var manager = fixture.CreateManager();
            manager.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(11, manager.Get(ResourceType.Gold));
            Assert.AreEqual(12, manager.Get(ResourceType.Energy));
            Assert.AreEqual(13, manager.Get(ResourceType.Gems));
        }

        [Test]
        public void Add_ServerSuccess_AppliesSnapshotAndPersistsToSave()
        {
            AdjustResourceCommand capturedCommand = null;
            var fixture = CreateFixture(command =>
            {
                capturedCommand = command;
                return UniTask.FromResult(new AdjustResourceResponse
                {
                    Success = true,
                    Resources = new ResourceSnapshotDto { Gold = 100, Energy = 20, Gems = 5 }
                });
            });

            fixture.SaveService
                .UpdateModuleAsync(data => data.Resources, resources =>
                {
                    resources.Gold = 1;
                    resources.Energy = 2;
                    resources.Gems = 3;
                }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var manager = fixture.CreateManager();
            manager.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            manager.Add(ResourceType.Gold, 25, ResourceManager.RewardGrantReason, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.NotNull(capturedCommand);
            Assert.AreEqual("player-test", capturedCommand.PlayerId);
            Assert.AreEqual("Gold", capturedCommand.ResourceId);
            Assert.AreEqual(25, capturedCommand.Delta);
            Assert.AreEqual(ResourceManager.RewardGrantReason, capturedCommand.Reason);

            Assert.AreEqual(100, manager.Get(ResourceType.Gold));
            Assert.AreEqual(20, manager.Get(ResourceType.Energy));
            Assert.AreEqual(5, manager.Get(ResourceType.Gems));

            var saved = fixture.SaveService.GetReadonlyModuleAsync(data => data.Resources, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            Assert.AreEqual(100, saved.Gold);
            Assert.AreEqual(20, saved.Energy);
            Assert.AreEqual(5, saved.Gems);
        }

        [Test]
        public void Remove_ServerSuccess_SendsNegativeDeltaAndReturnsTrue()
        {
            AdjustResourceCommand capturedCommand = null;
            var fixture = CreateFixture(command =>
            {
                capturedCommand = command;
                return UniTask.FromResult(new AdjustResourceResponse
                {
                    Success = true,
                    Resources = new ResourceSnapshotDto { Gold = 1, Energy = 4, Gems = 1 }
                });
            });

            fixture.SaveService
                .UpdateModuleAsync(data => data.Resources, resources =>
                {
                    resources.Gold = 1;
                    resources.Energy = 10;
                    resources.Gems = 1;
                }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var manager = fixture.CreateManager();
            manager.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            var removed = manager.Remove(ResourceType.Energy, 6, ResourceManager.CheatRemoveReason, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.IsTrue(removed);
            Assert.NotNull(capturedCommand);
            Assert.AreEqual("Energy", capturedCommand.ResourceId);
            Assert.AreEqual(-6, capturedCommand.Delta);
            Assert.AreEqual(ResourceManager.CheatRemoveReason, capturedCommand.Reason);
            Assert.AreEqual(4, manager.Get(ResourceType.Energy));
        }

        [Test]
        public void Add_ServerRejects_ThrowsAndDoesNotMutate()
        {
            var fixture = CreateFixture(_ => UniTask.FromResult(new AdjustResourceResponse
            {
                Success = false,
                ErrorCode = "NOT_ENOUGH",
                ErrorMessage = "Not enough resources",
                Resources = null
            }));

            fixture.SaveService
                .UpdateModuleAsync(data => data.Resources, resources =>
                {
                    resources.Gold = 4;
                    resources.Energy = 5;
                    resources.Gems = 6;
                }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var manager = fixture.CreateManager();
            manager.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.Throws<InvalidOperationException>(() =>
                manager.Add(ResourceType.Gold, 1, ResourceManager.RewardGrantReason, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());

            Assert.AreEqual(4, manager.Get(ResourceType.Gold));
            Assert.AreEqual(5, manager.Get(ResourceType.Energy));
            Assert.AreEqual(6, manager.Get(ResourceType.Gems));
        }

        [Test]
        public void Add_ApiThrows_ThrowsAndDoesNotMutate()
        {
            var fixture = CreateFixture(_ => UniTask.FromException<AdjustResourceResponse>(new InvalidOperationException("network")));

            fixture.SaveService
                .UpdateModuleAsync(data => data.Resources, resources =>
                {
                    resources.Gold = 7;
                    resources.Energy = 8;
                    resources.Gems = 9;
                }, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var manager = fixture.CreateManager();
            manager.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.Throws<InvalidOperationException>(() =>
                manager.Add(ResourceType.Gold, 1, ResourceManager.RewardGrantReason, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult());

            Assert.AreEqual(7, manager.Get(ResourceType.Gold));
            Assert.AreEqual(8, manager.Get(ResourceType.Energy));
            Assert.AreEqual(9, manager.Get(ResourceType.Gems));
        }

        private static TestFixtureContext CreateFixture(Func<AdjustResourceCommand, UniTask<AdjustResourceResponse>> handler)
        {
            var storage = new InMemorySaveStorage();
            var saveService = new SaveService(storage, new SaveMigrationService());
            var api = new StubResourceAdjustApi(handler);
            return new TestFixtureContext(saveService, api);
        }

        private sealed class TestFixtureContext
        {
            private readonly IResourceAdjustApi _resourceAdjustApi;

            public TestFixtureContext(SaveService saveService, IResourceAdjustApi resourceAdjustApi)
            {
                SaveService = saveService;
                _resourceAdjustApi = resourceAdjustApi;
            }

            public SaveService SaveService { get; }

            public ResourceManager CreateManager()
            {
                return new ResourceManager(SaveService, new StubPlayerIdentityProvider(), _resourceAdjustApi);
            }
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
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }

            public UniTask<AdjustResourceResponse> AdjustAsync(AdjustResourceCommand command, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return _handler(command);
            }
        }

        private sealed class InMemorySaveStorage : ISaveStorage
        {
            private string _payload;
            private long _lastModified;

            public UniTask SaveAsync(string data, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _payload = data;
                _lastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return UniTask.CompletedTask;
            }

            public UniTask<string> LoadAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(_payload ?? string.Empty);
            }

            public bool Exists()
            {
                return !string.IsNullOrWhiteSpace(_payload);
            }

            public UniTask DeleteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _payload = null;
                return UniTask.CompletedTask;
            }

            public UniTask<long> GetLastModifiedTimestampAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(_lastModified);
            }
        }
    }
}
