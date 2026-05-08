using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class FishingConfigValidationTests
    {
        [Test]
        public void StartupFishingJson_ParsesAndValidates()
        {
            var provider = new JsonFishingConfigProvider(
                new AssetFileFishingContentSource(),
                new FishingConfigValidator());

            var data = provider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(data.Fish.Count, Is.EqualTo(43));
            Assert.That(data.Zones.Count, Is.EqualTo(8));
            Assert.That(data.Lures.Count, Is.EqualTo(5));
            Assert.That(data.Items.Count, Is.EqualTo(48));
        }

        [Test]
        public void FishSelector_ExcludesEventOnlyFish_WhenEventIsInactive()
        {
            var selector = new FishSelector(new FixedFishingRandom(0));
            var fish = new List<FishConfig>
            {
                CreateFish("regular", false),
                CreateFish("event_only", true)
            };

            var available = selector.GetAvailableFish(fish, "green_lure", "coastal", new List<string>());

            Assert.That(available.Count, Is.EqualTo(1));
            Assert.That(available[0].Id, Is.EqualTo("regular"));
        }

        [Test]
        public void FishWeightService_UsesConfiguredThresholds()
        {
            var service = new FishWeightService(new FixedFishingRandom(0));
            var fish = CreateFish("test", false);

            Assert.That(service.GetState(fish, 0.9f), Is.EqualTo(FishWeightState.Common));
            Assert.That(service.GetState(fish, 2.0f), Is.EqualTo(FishWeightState.Rare));
            Assert.That(service.GetState(fish, 3.0f), Is.EqualTo(FishWeightState.Epic));
            Assert.That(service.GetState(fish, 4.0f), Is.EqualTo(FishWeightState.Legendary));
        }

        private static FishConfig CreateFish(string id, bool eventOnly)
        {
            return new FishConfig
            {
                Id = id,
                DisplayName = id,
                AvailableLureIds = new List<string> { "green_lure" },
                WaterBodyTypes = new List<string> { "coastal" },
                SpawnWeight = 1,
                EventOnly = eventOnly,
                EventIds = eventOnly ? new List<string> { "fishing_event" } : new List<string>(),
                WeightThresholds = new FishWeightThresholds
                {
                    Common = 1,
                    Rare = 2,
                    Epic = 3,
                    Legendary = 4
                }
            };
        }

        private sealed class AssetFileFishingContentSource : IFishingConfigContentSource
        {
            public UniTask<string> LoadJsonAsync(string relativePath, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                var path = Path.Combine(Application.dataPath, "StreamingAssets", "Fishing", relativePath);
                return UniTask.FromResult(File.ReadAllText(path));
            }
        }

        private sealed class FixedFishingRandom : IFishingRandom
        {
            private readonly double _value;

            public FixedFishingRandom(double value)
            {
                _value = value;
            }

            public double NextDouble()
            {
                return _value;
            }
        }
    }
}
