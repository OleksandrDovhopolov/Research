using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Newtonsoft.Json;

namespace Game.Fishing
{
    public sealed class SaveBackedFishBookService : IFishBookService
    {
        private const string SaveKey = "fishing_book";
        private readonly SaveService _saveService;

        public SaveBackedFishBookService(SaveService saveService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public async UniTask RegisterCatchAsync(FishingCatchResult result, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.FishId))
                return;

            await _saveService.UpdateModuleAsync(data => data.CustomModulesJson, modules =>
            {
                var saveData = Deserialize(modules);
                var progress = saveData.Progress.FirstOrDefault(x => string.Equals(x.FishId, result.FishId, StringComparison.Ordinal));
                if (progress == null)
                {
                    progress = new FishBookProgress
                    {
                        FishId = result.FishId,
                        IsDiscovered = true,
                        IsNew = true,
                        CaughtCount = 0,
                        BestWeight = 0f,
                        UnlockedWeightStates = new List<string>()
                    };
                    saveData.Progress.Add(progress);
                }

                progress.IsDiscovered = true;
                progress.CaughtCount += 1;
                progress.BestWeight = Math.Max(progress.BestWeight, result.Weight);

                var stateId = ToStateId(result.State);
                if (!progress.UnlockedWeightStates.Contains(stateId))
                    progress.UnlockedWeightStates.Add(stateId);

                modules[SaveKey] = JsonConvert.SerializeObject(saveData);
            }, ct);
        }

        public async UniTask<FishBookProgress> GetProgressAsync(string fishId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fishId))
                return null;

            var modules = await _saveService.GetReadonlyModuleAsync(data => data.CustomModulesJson, ct);
            var saveData = Deserialize(modules);
            return saveData.Progress.FirstOrDefault(x => string.Equals(x.FishId, fishId, StringComparison.Ordinal));
        }

        public async UniTask MarkAsViewedAsync(string fishId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fishId))
                return;

            await _saveService.UpdateModuleAsync(data => data.CustomModulesJson, modules =>
            {
                var saveData = Deserialize(modules);
                var progress = saveData.Progress.FirstOrDefault(x => string.Equals(x.FishId, fishId, StringComparison.Ordinal));
                if (progress == null)
                    return;

                progress.IsNew = false;
                modules[SaveKey] = JsonConvert.SerializeObject(saveData);
            }, ct);
        }

        private static FishBookSaveData Deserialize(Dictionary<string, string> modules)
        {
            if (modules == null || !modules.TryGetValue(SaveKey, out var json) || string.IsNullOrWhiteSpace(json))
                return new FishBookSaveData();

            try
            {
                return JsonConvert.DeserializeObject<FishBookSaveData>(json) ?? new FishBookSaveData();
            }
            catch
            {
                return new FishBookSaveData();
            }
        }

        private static string ToStateId(FishWeightState state)
        {
            return state switch
            {
                FishWeightState.Legendary => "legendary",
                FishWeightState.Epic => "epic",
                FishWeightState.Rare => "rare",
                _ => "common"
            };
        }

        private sealed class FishBookSaveData
        {
            [JsonProperty("progress")] public List<FishBookProgress> Progress = new();
        }
    }
}
