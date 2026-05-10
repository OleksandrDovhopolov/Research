using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class CaughtFishFlowTests
    {
        [Test]
        public void ConfigBackedFishCatchResolver_ReturnsSuccessfulCatch_ForValidFishId()
        {
            var resolver = new ConfigBackedFishCatchResolver(
                new StubFishingConfigProvider(CreateStaticData()),
                new StubFishWeightService(new FishWeightRollResult(4.2f, FishWeightState.Legendary)));

            var result = resolver.ResolveCatchAsync("roach", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.True);
            Assert.That(result.FishId, Is.EqualTo("roach"));
            Assert.That(result.ItemId, Is.EqualTo("item_roach"));
            Assert.That(result.Weight, Is.EqualTo(4.2f));
            Assert.That(result.State, Is.EqualTo(FishWeightState.Legendary));
        }

        [Test]
        public void ConfigBackedFishCatchResolver_ReturnsConfigInvalid_ForUnknownFishId()
        {
            var resolver = new ConfigBackedFishCatchResolver(
                new StubFishingConfigProvider(CreateStaticData()),
                new StubFishWeightService(new FishWeightRollResult(1f, FishWeightState.Common)));

            var result = resolver.ResolveCatchAsync("missing_fish", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(FishingError.ConfigInvalid));
        }

        [Test]
        public void CaughtFishService_PersistsAndPresentsSuccessfulCatch()
        {
            var bookService = new StubFishBookService();
            var presenter = new StubCaughtFishPresenter();
            var service = new CaughtFishService(bookService, presenter);
            var catchResult = FishingCatchResult.Ok("roach", "item_roach", 2.75f, FishWeightState.Epic);

            var returnedResult = service.HandleCatchAsync(catchResult, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(returnedResult, Is.SameAs(catchResult));
            Assert.That(bookService.Registered, Has.Count.EqualTo(1));
            Assert.That(presenter.CallCount, Is.EqualTo(1));
            Assert.That(presenter.LastResult, Is.SameAs(catchResult));
            Assert.That(presenter.LastProgress, Is.Not.Null);
            Assert.That(presenter.LastProgress.CaughtCount, Is.EqualTo(1));
        }

        [Test]
        public void LoggingCaughtFishPresenter_LogsCatchPayload()
        {
            var presenter = new LoggingCaughtFishPresenter();
            var result = FishingCatchResult.Ok("pike", "item_pike", 3.4f, FishWeightState.Rare);
            var progress = new FishBookProgress
            {
                FishId = "pike",
                IsDiscovered = true,
                IsNew = true,
                CaughtCount = 2,
                BestWeight = 4.1f,
                UnlockedWeightStates = new List<string> { "common", "rare" }
            };

            LogAssert.Expect(LogType.Log, new Regex(@"\[CaughtFishPresenter\] FishId='pike'.*State=Rare.*Weight=3\.4.*BestWeight=4\.1.*UnlockedStates=\[common, rare\]"));
            presenter.Present(result, progress);
        }

        private static FishingStaticData CreateStaticData()
        {
            return new FishingStaticData(
                new List<FishConfig>
                {
                    new()
                    {
                        Id = "roach",
                        DisplayName = "Roach",
                        WeightThresholds = new FishWeightThresholds
                        {
                            Common = 1f,
                            Rare = 2f,
                            Epic = 3f,
                            Legendary = 4f
                        }
                    }
                },
                Array.Empty<FishingZoneConfig>(),
                Array.Empty<WaterBodyTypeConfig>(),
                Array.Empty<LureConfig>(),
                Array.Empty<FishingItemConfig>(),
                new FishingSettingsConfigRoot(),
                Array.Empty<string>());
        }

        private sealed class StubFishingConfigProvider : IFishingConfigProvider
        {
            private readonly FishingStaticData _data;

            public StubFishingConfigProvider(FishingStaticData data)
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

        private sealed class StubFishWeightService : IFishWeightService
        {
            private readonly FishWeightRollResult _rollResult;

            public StubFishWeightService(FishWeightRollResult rollResult)
            {
                _rollResult = rollResult;
            }

            public FishWeightRollResult RollWeight(FishConfig fishConfig)
            {
                return _rollResult;
            }

            public FishWeightState GetState(FishConfig fishConfig, float weight)
            {
                return _rollResult.State;
            }
        }

        private sealed class StubFishBookService : IFishBookService
        {
            public List<FishingCatchResult> Registered { get; } = new();
            private readonly Dictionary<string, FishBookProgress> _progressByFishId = new(StringComparer.Ordinal);

            public UniTask RegisterCatchAsync(FishingCatchResult result, CancellationToken ct = default)
            {
                Registered.Add(result);

                if (!_progressByFishId.TryGetValue(result.FishId, out var progress))
                {
                    progress = new FishBookProgress
                    {
                        FishId = result.FishId,
                        IsDiscovered = true,
                        IsNew = true,
                        UnlockedWeightStates = new List<string>()
                    };
                    _progressByFishId[result.FishId] = progress;
                }

                progress.CaughtCount += 1;
                progress.BestWeight = Math.Max(progress.BestWeight, result.Weight);
                var stateId = result.State.ToString().ToLowerInvariant();
                if (!progress.UnlockedWeightStates.Contains(stateId))
                    progress.UnlockedWeightStates.Add(stateId);

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

        private sealed class StubCaughtFishPresenter : ICaughtFishPresenter
        {
            public int CallCount { get; private set; }
            public FishingCatchResult LastResult { get; private set; }
            public FishBookProgress LastProgress { get; private set; }

            public void Present(FishingCatchResult result, FishBookProgress progress)
            {
                CallCount++;
                LastResult = result;
                LastProgress = progress;
            }
        }
    }
}
