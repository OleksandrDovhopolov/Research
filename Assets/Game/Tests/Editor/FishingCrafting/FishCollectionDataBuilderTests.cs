using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using NUnit.Framework;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class FishCollectionDataBuilderTests
    {
        [Test]
        public void BuildAsync_ReturnsEntryForEachFish()
        {
            var builder = CreateBuilder();

            var args = builder.BuildAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(args.Entries.Count, Is.EqualTo(2));
            Assert.That(args.Entries[0].FishId, Is.EqualTo("roach"));
            Assert.That(args.Entries[1].FishId, Is.EqualTo("pike"));
        }

        [Test]
        public void BuildAsync_UsesRuntimeWeightFormulaForMinAndMax()
        {
            var builder = CreateBuilder();

            var args = builder.BuildAsync(CancellationToken.None).GetAwaiter().GetResult();
            var roach = args.Entries[0];

            Assert.That(roach.MinWeight, Is.EqualTo(0.75f));
            Assert.That(roach.MaxWeight, Is.EqualTo(5f));
        }

        [Test]
        public void BuildAsync_ResolvesWaterBodyTypeNames()
        {
            var builder = CreateBuilder();

            var args = builder.BuildAsync(CancellationToken.None).GetAwaiter().GetResult();
            var pike = args.Entries[1];

            Assert.That(pike.WaterBodyTypesText, Is.EqualTo("Fresh Water, River"));
        }

        [Test]
        public void BuildAsync_UsesFishingItemType()
        {
            var builder = CreateBuilder();

            var args = builder.BuildAsync(CancellationToken.None).GetAwaiter().GetResult();
            var roach = args.Entries[0];

            Assert.That(roach.ItemType, Is.EqualTo("fish_common"));
            Assert.That(roach.ItemId, Is.EqualTo("item_roach"));
        }

        [Test]
        public void BuildAsync_KeepsEntryWhenProgressIsMissing()
        {
            var builder = CreateBuilder();

            var args = builder.BuildAsync(CancellationToken.None).GetAwaiter().GetResult();
            var pike = args.Entries[1];

            Assert.That(pike.Progress, Is.Null);
            Assert.That(pike.DisplayName, Is.EqualTo("Pike"));
        }

        private static FishCollectionDataBuilder CreateBuilder()
        {
            var data = new FishingStaticData(
                new List<FishConfig>
                {
                    new()
                    {
                        Id = "roach",
                        DisplayName = "Roach",
                        WaterBodyTypes = new List<string> { "lake" },
                        BehaviorType = "calm",
                        WeightThresholds = new FishWeightThresholds
                        {
                            Common = 1f,
                            Rare = 2f,
                            Epic = 3f,
                            Legendary = 4f
                        }
                    },
                    new()
                    {
                        Id = "pike",
                        DisplayName = "Pike",
                        WaterBodyTypes = new List<string> { "fresh", "river" },
                        BehaviorType = "aggressive",
                        WeightThresholds = new FishWeightThresholds
                        {
                            Common = 2f,
                            Rare = 4f,
                            Epic = 6f,
                            Legendary = 8f
                        }
                    }
                },
                new List<FishingZoneConfig>(),
                new List<WaterBodyTypeConfig>
                {
                    new() { Id = "lake", DisplayName = "Lake" },
                    new() { Id = "fresh", DisplayName = "Fresh Water" },
                    new() { Id = "river", DisplayName = "River" }
                },
                new List<LureConfig>(),
                new List<FishingItemConfig>
                {
                    new() { Id = "item_roach", Type = "fish_common" },
                    new() { Id = "item_pike", Type = "fish_predator" }
                },
                new FishingSettingsConfigRoot(),
                new string[0]);

            return new FishCollectionDataBuilder(
                new FakeFishingConfigProvider(data),
                new FakeFishBookService(new Dictionary<string, FishBookProgress>
                {
                    ["roach"] = new FishBookProgress
                    {
                        FishId = "roach",
                        IsDiscovered = true,
                        CaughtCount = 3,
                        BestWeight = 2.34f
                    }
                }));
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

        private sealed class FakeFishBookService : IFishBookService
        {
            private readonly IReadOnlyDictionary<string, FishBookProgress> _progressByFishId;

            public FakeFishBookService(IReadOnlyDictionary<string, FishBookProgress> progressByFishId)
            {
                _progressByFishId = progressByFishId;
            }

            public UniTask RegisterCatchAsync(FishingCatchResult result, CancellationToken ct = default)
            {
                return UniTask.CompletedTask;
            }

            public UniTask<FishBookProgress> GetProgressAsync(string fishId, CancellationToken ct = default)
            {
                _progressByFishId.TryGetValue(fishId, out var progress);
                return UniTask.FromResult(progress);
            }

            public UniTask MarkAsViewedAsync(string fishId, CancellationToken ct = default)
            {
                return UniTask.CompletedTask;
            }
        }
    }
}
