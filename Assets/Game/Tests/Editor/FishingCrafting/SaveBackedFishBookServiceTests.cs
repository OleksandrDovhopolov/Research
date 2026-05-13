using System;
using System.IO;
using System.Linq;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class SaveBackedFishBookServiceTests
    {
        private const string TestFileName = "fishing_book_tests.json";
        private string _testFilePath;
        private string _backupFilePath;

        [SetUp]
        public void SetUp()
        {
            _testFilePath = Path.Combine(Application.persistentDataPath, TestFileName);
            _backupFilePath = _testFilePath + ".bak";
            TryDelete(_testFilePath);
            TryDelete(_backupFilePath);
            TryDelete(_testFilePath + ".tmp");
        }

        [TearDown]
        public void TearDown()
        {
            TryDelete(_testFilePath);
            TryDelete(_backupFilePath);
            TryDelete(_testFilePath + ".tmp");
        }

        [Test]
        public void RegisterCatchAsync_CreatesAndUpdatesRootFishingSave()
        {
            using var saveService = CreateSaveService();
            saveService.LoadAllAsync(CancellationToken.None).GetAwaiter().GetResult();
            var service = new SaveBackedFishBookService(saveService);

            service.RegisterCatchAsync(FishingCatchResult.Ok("roach", "item_roach", 1.25f, FishWeightState.Common), CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            service.RegisterCatchAsync(FishingCatchResult.Ok("roach", "item_roach", 3.5f, FishWeightState.Epic), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var fishing = saveService.GetReadonlyModuleAsync(data => data.Fishing, CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(fishing, Is.Not.Null);
            Assert.That(fishing.CaughtFish, Has.Count.EqualTo(1));
            var caughtFish = fishing.CaughtFish.Single();
            Assert.That(caughtFish.FishId, Is.EqualTo("roach"));
            Assert.That(caughtFish.CaughtCount, Is.EqualTo(2));
            Assert.That(caughtFish.BestWeight, Is.EqualTo(3.5f));
            Assert.That(caughtFish.CaughtStatesMask, Is.EqualTo((1 << 0) | (1 << 2)));
            Assert.That(caughtFish.IsNew, Is.True);

            var progress = service.GetProgressAsync("roach", CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(progress, Is.Not.Null);
            Assert.That(progress.IsDiscovered, Is.True);
            Assert.That(progress.CaughtCount, Is.EqualTo(2));
            Assert.That(progress.BestWeight, Is.EqualTo(3.5f));
            Assert.That(progress.UnlockedWeightStates, Is.EqualTo(new[] { "common", "epic" }));
        }

        [Test]
        public void MarkAsViewedAsync_ClearsIsNewFlag()
        {
            using var saveService = CreateSaveService();
            saveService.LoadAllAsync(CancellationToken.None).GetAwaiter().GetResult();
            var service = new SaveBackedFishBookService(saveService);

            service.RegisterCatchAsync(FishingCatchResult.Ok("pike", "item_pike", 2.5f, FishWeightState.Rare), CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            service.MarkAsViewedAsync("pike", CancellationToken.None).GetAwaiter().GetResult();

            var progress = service.GetProgressAsync("pike", CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(progress, Is.Not.Null);
            Assert.That(progress.IsNew, Is.False);
        }

        [Test]
        public void GetProgressAsync_IgnoresLegacyCustomModulePayload()
        {
            using var saveService = CreateSaveService();
            saveService.LoadAllAsync(CancellationToken.None).GetAwaiter().GetResult();
            saveService.UpdateModuleAsync(data => data.CustomModulesJson, modules =>
            {
                modules["fishing_book"] = "{\"progress\":[{\"FishId\":\"legacy_roach\",\"IsDiscovered\":true,\"IsNew\":true,\"CaughtCount\":5,\"BestWeight\":9.9,\"UnlockedWeightStates\":[\"legendary\"]}]}";
            }, CancellationToken.None).GetAwaiter().GetResult();

            var service = new SaveBackedFishBookService(saveService);
            var progress = service.GetProgressAsync("legacy_roach", CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(progress, Is.Null);
            var fishing = saveService.GetReadonlyModuleAsync(data => data.Fishing, CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(fishing.CaughtFish, Is.Empty);
        }

        private static SaveService CreateSaveService()
        {
            return new SaveService(new LocalDiskStorage(TestFileName), new SaveMigrationService());
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
