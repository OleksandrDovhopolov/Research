using System;
using System.Collections.Generic;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using Game.Crafting;
using Infrastructure;
using Newtonsoft.Json;
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
        public void StartCraftAsync_SavesTaskInCraftingModule()
        {
            var fixture = CreateFixture();

            var start = fixture.Service.StartCraftAsync("craft_green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            var saveData = fixture.SaveService.GetReadonlyModuleAsync(data => data.Crafting, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(start.Success, Is.True);
            Assert.That(saveData.Tasks, Has.Count.EqualTo(1));
            Assert.That(saveData.Tasks[0].TaskId, Is.EqualTo(start.TaskId.Value));
            Assert.That(saveData.Tasks[0].RecipeId, Is.EqualTo("craft_green_lure"));
            Assert.That(saveData.Tasks[0].StationId, Is.EqualTo(CraftingStationIds.LureCrafting));
        }

        [Test]
        public void GetActiveTasksAsync_RestoresTaskFromSaveService()
        {
            var fixture = CreateFixture();
            var start = fixture.Service.StartCraftAsync("craft_green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            var recreated = CreateService(fixture.SaveService, fixture.RewardApplier, fixture.Clock);

            var tasks = recreated.GetActiveTasksAsync(CraftingStationIds.LureCrafting, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(start.Success, Is.True);
            Assert.That(tasks, Has.Count.EqualTo(1));
            Assert.That(tasks[0].Id.Value, Is.EqualTo(start.TaskId.Value));
            Assert.That(tasks[0].Recipe.Id, Is.EqualTo("craft_green_lure"));
        }

        [Test]
        public void GetActiveTasksAsync_RestoresTaskAfterSaveServiceRecreated()
        {
            var fixture = CreateFixture();
            var start = fixture.Service.StartCraftAsync("craft_green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            fixture.SaveService.SaveAllAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var recreatedSaveService = new SaveService(fixture.Storage, new SaveMigrationService());
            var recreated = CreateService(recreatedSaveService, fixture.RewardApplier, fixture.Clock);

            var tasks = recreated.GetActiveTasksAsync(CraftingStationIds.LureCrafting, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(start.Success, Is.True);
            Assert.That(tasks, Has.Count.EqualTo(1));
            Assert.That(tasks[0].Id.Value, Is.EqualTo(start.TaskId.Value));
            Assert.That(tasks[0].Recipe.Id, Is.EqualTo("craft_green_lure"));
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
            Assert.That(fixture.RewardApplier.GetAmount("item_green_lure"), Is.EqualTo(0));
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
            Assert.That(fixture.RewardApplier.GetAmount("item_green_lure"), Is.EqualTo(1));
            Assert.That(fixture.Service.GetActiveTasksAsync(CraftingStationIds.LureCrafting, CancellationToken.None).GetAwaiter().GetResult(), Is.Empty);
        }

        [Test]
        public void CompleteAndCollectAsync_BeforeComplete_GrantsOutput()
        {
            var fixture = CreateFixture();
            var start = fixture.Service.StartCraftAsync("craft_green_lure", CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var collect = fixture.Service.CompleteAndCollectAsync(start.TaskId, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(collect.Success, Is.True);
            Assert.That(collect.OutputItemId, Is.EqualTo("item_green_lure"));
            Assert.That(fixture.RewardApplier.GetAmount("item_green_lure"), Is.EqualTo(1));
            Assert.That(fixture.Service.GetActiveTasksAsync(CraftingStationIds.LureCrafting, CancellationToken.None).GetAwaiter().GetResult(), Is.Empty);
        }

        private static Fixture CreateFixture()
        {
            var data = new CraftingStaticData(new List<CraftingRecipeConfig>
            {
                new()
                {
                    Id = "craft_green_lure",
                    DisplayName = "Craft Green Lure",
                    StationId = CraftingStationIds.LureCrafting,
                    OutputItemId = "item_green_lure",
                    OutputCount = 1,
                    CraftTimeSeconds = 5,
                    Ingredients = new List<CraftingIngredientConfig>(),
                    Requirements = new List<CraftingRequirementConfig>(),
                    IsEnabled = true
                }
            });
            var storage = InMemorySaveStorage.CreateWithDefaultSave();
            var saveService = new SaveService(storage, new SaveMigrationService());
            var rewardApplier = new FakeCraftingRewardApplier();
            var clock = new FakeCraftingClock { UtcNow = DateTimeOffset.UtcNow };
            var service = CreateService(saveService, rewardApplier, clock, data);
            return new Fixture(service, saveService, storage, rewardApplier, clock);
        }

        private static CraftingService CreateService(
            SaveService saveService,
            FakeCraftingRewardApplier rewardApplier,
            FakeCraftingClock clock,
            CraftingStaticData data = null)
        {
            data ??= new CraftingStaticData(new List<CraftingRecipeConfig>
            {
                new()
                {
                    Id = "craft_green_lure",
                    DisplayName = "Craft Green Lure",
                    StationId = CraftingStationIds.LureCrafting,
                    OutputItemId = "item_green_lure",
                    OutputCount = 1,
                    CraftTimeSeconds = 5,
                    Ingredients = new List<CraftingIngredientConfig>(),
                    Requirements = new List<CraftingRequirementConfig>(),
                    IsEnabled = true
                }
            });

            return new CraftingService(new FakeCraftingConfigProvider(data), rewardApplier, saveService, clock);
        }

        private sealed class Fixture
        {
            public Fixture(
                CraftingService service,
                SaveService saveService,
                InMemorySaveStorage storage,
                FakeCraftingRewardApplier rewardApplier,
                FakeCraftingClock clock)
            {
                Service = service;
                SaveService = saveService;
                Storage = storage;
                RewardApplier = rewardApplier;
                Clock = clock;
            }

            public CraftingService Service { get; }
            public SaveService SaveService { get; }
            public InMemorySaveStorage Storage { get; }
            public FakeCraftingRewardApplier RewardApplier { get; }
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

        private sealed class FakeCraftingRewardApplier : ICraftingRewardApplier
        {
            private readonly Dictionary<string, int> _items = new();

            public int GetAmount(string itemId)
            {
                return _items.TryGetValue(itemId, out var amount) ? amount : 0;
            }

            public UniTask ApplyAsync(string outputItemId, int outputCount, CancellationToken ct = default)
            {
                _items[outputItemId] = GetAmount(outputItemId) + outputCount;
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeCraftingClock : IClock
        {
            public DateTimeOffset UtcNow { get; set; }
        }

        private sealed class InMemorySaveStorage : ISaveStorage
        {
            private string _json;

            public static InMemorySaveStorage CreateWithDefaultSave()
            {
                return new InMemorySaveStorage
                {
                    _json = JsonConvert.SerializeObject(GameSaveData.CreateDefault(2, "crafting-tests"))
                };
            }

            public UniTask SaveAsync(string data, CancellationToken cancellationToken)
            {
                _json = data;
                return UniTask.CompletedTask;
            }

            public UniTask<string> LoadAsync(CancellationToken cancellationToken)
            {
                return UniTask.FromResult(_json);
            }

            public bool Exists()
            {
                return !string.IsNullOrWhiteSpace(_json);
            }

            public UniTask DeleteAsync(CancellationToken cancellationToken)
            {
                _json = null;
                return UniTask.CompletedTask;
            }

            public UniTask<long> GetLastModifiedTimestampAsync(CancellationToken cancellationToken)
            {
                return UniTask.FromResult(0L);
            }
        }
    }
}
