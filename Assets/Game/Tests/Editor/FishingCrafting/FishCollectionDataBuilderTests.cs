using System.Collections.Generic;
using System.Linq;
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

            Assert.That(roach.ItemType, Is.EqualTo("fish"));
            Assert.That(roach.SpriteAddress, Is.EqualTo("roach"));
        }

        [Test]
        public void BuildAsync_ResolvesLureViewData_AndBestCaughtWeight()
        {
            var builder = CreateBuilder();

            var args = builder.BuildAsync(CancellationToken.None).GetAwaiter().GetResult();
            var roach = args.Entries[0];

            Assert.That(roach.BestCaughtWeight, Is.EqualTo(2.34f));
            Assert.That(roach.IsDiscovered, Is.True);
            Assert.That(roach.Lures.Select(x => x.LureId), Is.EqualTo(new[] { "red_lure", "green_lure" }));
            Assert.That(roach.Lures.Select(x => x.SpriteAddress), Is.EqualTo(new[] { "red_lure", "green_lure" }));
        }

        [Test]
        public void BuildAsync_KeepsEntryWhenProgressIsMissing()
        {
            var builder = CreateBuilder();

            var args = builder.BuildAsync(CancellationToken.None).GetAwaiter().GetResult();
            var pike = args.Entries[1];

            Assert.That(pike.Progress, Is.Null);
            Assert.That(pike.DisplayName, Is.EqualTo("Pike"));
            Assert.That(pike.IsDiscovered, Is.False);
            Assert.That(pike.BestCaughtWeight, Is.EqualTo(0f));
        }

        [Test]
        public void BuildAsync_SkipsEventOnlyFish()
        {
            var builder = CreateBuilder();

            var args = builder.BuildAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(args.Entries.Count, Is.EqualTo(2));
            Assert.That(args.Entries.Any(x => x.FishId == "event_fish"), Is.False);
        }

        private static FishCollectionDataBuilder CreateBuilder()
        {
            return new FishCollectionDataBuilder(
                new FakeFishingConfigProvider(),
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
            public UniTask<FishingStaticData> LoadAsync(CancellationToken ct)
            {
                return UniTask.FromResult(new FishingStaticData(
                    new List<FishConfig>
                    {
                        new()
                        {
                            Id = "roach",
                            DisplayName = "Roach",
                            AvailableLureIds = new List<string> { "red_lure", "green_lure" },
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
                            AvailableLureIds = new List<string> { "green_lure" },
                            WaterBodyTypes = new List<string> { "fresh", "river" },
                            BehaviorType = "aggressive",
                            WeightThresholds = new FishWeightThresholds
                            {
                                Common = 2f,
                                Rare = 4f,
                                Epic = 6f,
                                Legendary = 8f
                            }
                        },
                        new()
                        {
                            Id = "event_fish",
                            DisplayName = "Event Fish",
                            WaterBodyTypes = new List<string> { "lake" },
                            BehaviorType = "heavy",
                            EventOnly = true,
                            EventIds = new List<string> { "spring_event" },
                            WeightThresholds = new FishWeightThresholds
                            {
                                Common = 5f,
                                Rare = 6f,
                                Epic = 7f,
                                Legendary = 8f
                            }
                        }
                    },
                    System.Array.Empty<FishingZoneConfig>(),
                    new List<WaterBodyTypeConfig>
                    {
                        new() { Id = "lake", DisplayName = "Lake" },
                        new() { Id = "fresh", DisplayName = "Fresh Water" },
                        new() { Id = "river", DisplayName = "River" }
                    },
                    new List<LureConfig>
                    {
                        new() { Id = "red_lure", DisplayName = "Red Lure" },
                        new() { Id = "green_lure", DisplayName = "Green Lure" }
                    },
                    System.Array.Empty<FishingItemConfig>(),
                    new FishingSettingsConfigRoot(),
                    System.Array.Empty<string>()));
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
