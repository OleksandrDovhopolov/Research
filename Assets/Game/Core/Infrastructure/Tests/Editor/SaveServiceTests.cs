using System.Collections;
using System;
using System.IO;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Infrastructure.Tests.Editor
{
    public sealed class SaveServiceTests
    {
        private const string TestFileName = "global_save_tests.json";
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
            TryDelete(Path.Combine(Application.persistentDataPath, "inventory"));
            TryDelete(Path.Combine(Application.persistentDataPath, "event_cards"));
            TryDelete(Path.Combine(Application.persistentDataPath, "resources_data"));
        }

        [TearDown]
        public void TearDown()
        {
            TryDelete(_testFilePath);
            TryDelete(_backupFilePath);
            TryDelete(_testFilePath + ".tmp");
            TryDelete(Path.Combine(Application.persistentDataPath, "inventory"));
            TryDelete(Path.Combine(Application.persistentDataPath, "event_cards"));
            TryDelete(Path.Combine(Application.persistentDataPath, "resources_data"));
        }

        [UnityTest]
        public IEnumerator SaveAll_IsThreadSafe_WhenCalledConcurrently() => UniTask.ToCoroutine(async () =>
        {
            var service = CreateService();
            await service.LoadAllAsync(CancellationToken.None);
            await service.UpdateModuleAsync(data => data.Resources, resources =>
            {
                resources.Version = 1;
                resources.Gold = 10;
                resources.Energy = 20;
                resources.Gems = 5;
            }, CancellationToken.None);

            var task1 = service.SaveAllAsync(CancellationToken.None).AsTask();
            var task2 = service.SaveAllAsync(CancellationToken.None).AsTask();
            var task3 = service.SaveAllAsync(CancellationToken.None).AsTask();

            await UniTask.WhenAll(task1.AsUniTask(), task2.AsUniTask(), task3.AsUniTask());
            Assert.That(File.Exists(_testFilePath), Is.True);
        });

        [UnityTest]
        public IEnumerator LoadAll_FallsBackToDefault_WhenJsonCorrupted() => UniTask.ToCoroutine(async () =>
        {
            await File.WriteAllTextAsync(_testFilePath, "{not_valid_json", CancellationToken.None);

            var service = CreateService();
            var data = await service.LoadAllAsync(CancellationToken.None);

            Assert.That(data, Is.Not.Null);
            Assert.That(data.Meta, Is.Not.Null);
            Assert.That(data.Inventory, Is.Not.Null);
            Assert.That(data.Resources, Is.Not.Null);
        });

        [UnityTest]
        public IEnumerator Migration_LoadsLegacyFiles_IntoGlobalSave() => UniTask.ToCoroutine(async () =>
        {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "resources_data"));
            await File.WriteAllTextAsync(
                Path.Combine(Application.persistentDataPath, "resources_data", "resources.json"),
                "{\"Version\":1,\"Gold\":111,\"Energy\":22,\"Gems\":3}",
                CancellationToken.None);

            var service = CreateService();
            await service.LoadAllAsync(CancellationToken.None);
            await service.SaveAllAsync(CancellationToken.None);
            var resources = await service.GetReadonlyModuleAsync(data => new ResourcesModuleSaveData
            {
                Version = data.Resources.Version,
                Gold = data.Resources.Gold,
                Energy = data.Resources.Energy,
                Gems = data.Resources.Gems,
            }, CancellationToken.None);

            Assert.That(resources.Gold, Is.EqualTo(111));
            Assert.That(File.Exists(_testFilePath), Is.True);
        });

        [UnityTest]
        public IEnumerator LoadAll_InitializesFortuneWheel_WhenFieldMissingInJson() => UniTask.ToCoroutine(async () =>
        {
            const string legacyJsonWithoutFortuneWheel = "{\n" +
                                                        "  \"Meta\": { \"SchemaVersion\": 2, \"LastSaveTimestamp\": 0, \"Hash\": \"\", \"SaveId\": \"legacy\", \"Revision\": 0 },\n" +
                                                        "  \"Inventory\": { \"Owners\": [] },\n" +
                                                        "  \"CardCollections\": [],\n" +
                                                        "  \"EventStates\": [],\n" +
                                                        "  \"Resources\": { \"Version\": 1, \"Gold\": 0, \"Energy\": 0, \"Gems\": 0 },\n" +
                                                        "  \"CustomModulesJson\": {}\n" +
                                                        "}";
            await File.WriteAllTextAsync(_testFilePath, legacyJsonWithoutFortuneWheel, CancellationToken.None);

            var service = CreateService();
            var data = await service.LoadAllAsync(CancellationToken.None);

            Assert.That(data.FortuneWheel, Is.Not.Null);
            Assert.That(data.FortuneWheel.AvailableSpins, Is.EqualTo(0));
            Assert.That(data.FortuneWheel.LastResetTimestamp, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator LoadAll_ClampsFortuneWheelValues_WhenNegative() => UniTask.ToCoroutine(async () =>
        {
            const string invalidFortuneWheelJson = "{\n" +
                                                   "  \"Meta\": { \"SchemaVersion\": 2, \"LastSaveTimestamp\": 0, \"Hash\": \"\", \"SaveId\": \"invalid\", \"Revision\": 0 },\n" +
                                                   "  \"Inventory\": { \"Owners\": [] },\n" +
                                                   "  \"CardCollections\": [],\n" +
                                                   "  \"EventStates\": [],\n" +
                                                   "  \"Resources\": { \"Version\": 1, \"Gold\": 0, \"Energy\": 0, \"Gems\": 0 },\n" +
                                                   "  \"FortuneWheel\": { \"AvailableSpins\": -3, \"LastResetTimestamp\": -100 },\n" +
                                                   "  \"CustomModulesJson\": {}\n" +
                                                   "}";
            await File.WriteAllTextAsync(_testFilePath, invalidFortuneWheelJson, CancellationToken.None);

            var service = CreateService();
            var data = await service.LoadAllAsync(CancellationToken.None);

            Assert.That(data.FortuneWheel, Is.Not.Null);
            Assert.That(data.FortuneWheel.AvailableSpins, Is.EqualTo(0));
            Assert.That(data.FortuneWheel.LastResetTimestamp, Is.EqualTo(0));
        });

        private SaveService CreateService()
        {
            var storage = new LocalDiskStorage(TestFileName);
            var migration = new SaveMigrationService();
            return new SaveService(storage, migration);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return;
                }

                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
