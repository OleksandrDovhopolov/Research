using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using Infrastructure;

namespace Game.Crafting
{
    public sealed class CraftingService : ICraftingService
    {
        private const int DefaultStationSlotLimit = 1;

        private readonly ICraftingConfigProvider _configProvider;
        private readonly ICraftingRewardApplier _rewardApplier;
        private readonly SaveService _saveService;
        private readonly IClock _clock;

        public CraftingService(
            ICraftingConfigProvider configProvider,
            ICraftingRewardApplier rewardApplier,
            SaveService saveService,
            IClock clock)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _rewardApplier = rewardApplier ?? throw new ArgumentNullException(nameof(rewardApplier));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async UniTask<CraftStartResult> StartCraftAsync(string recipeId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(recipeId))
                return CraftStartResult.Fail(CraftingError.RecipeNotFound);

            var data = await _configProvider.LoadAsync(ct);
            if (!data.RecipesById.TryGetValue(recipeId, out var recipe))
                return CraftStartResult.Fail(CraftingError.RecipeNotFound);

            if (!recipe.IsEnabled)
                return CraftStartResult.Fail(CraftingError.RecipeDisabled);

            if (await GetActiveTaskCountAsync(recipe.StationId, ct) >= GetStationSlotLimit(recipe.StationId))
                return CraftStartResult.Fail(CraftingError.StationQueueFull);

            var now = _clock.UtcNow;
            var task = new CraftTask(
                new CraftTaskId(Guid.NewGuid().ToString("N")),
                recipe,
                now,
                now.AddSeconds(Math.Max(0, recipe.CraftTimeSeconds)));

            await _saveService.UpdateModuleAsync(data => data.Crafting, crafting =>
            {
                crafting.Tasks ??= new List<CraftTaskSaveData>();
                crafting.Tasks.Add(ToSaveData(task));
            }, ct);

            return CraftStartResult.Ok(task);
        }

        public async UniTask<IReadOnlyList<CraftTask>> GetActiveTasksAsync(string stationId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(stationId))
                return Array.Empty<CraftTask>();

            var data = await _configProvider.LoadAsync(ct);
            var saveData = await GetSaveDataAsync(ct);
            if (saveData.Tasks == null || saveData.Tasks.Count == 0)
                return Array.Empty<CraftTask>();

            var result = new List<CraftTask>();
            foreach (var taskSaveData in saveData.Tasks)
            {
                if (taskSaveData == null ||
                    !string.Equals(taskSaveData.StationId, stationId, StringComparison.Ordinal) ||
                    !data.RecipesById.TryGetValue(taskSaveData.RecipeId, out var recipe))
                {
                    continue;
                }

                result.Add(ToDomainTask(taskSaveData, recipe));
            }

            return result
                .OrderBy(task => task.StartedAtUtc)
                .ToArray();
        }

        public async UniTask<CraftTask> GetFirstActiveTaskAsync(string stationId, CancellationToken ct = default)
        {
            var tasks = await GetActiveTasksAsync(stationId, ct);
            return tasks.Count > 0 ? tasks[0] : null;
        }

        public async UniTask<CraftCollectResult> CollectAsync(CraftTaskId taskId, CancellationToken ct = default)
        {
            return await CollectInternalAsync(taskId, requireComplete: true, ct);
        }

        public async UniTask<CraftCollectResult> CompleteAndCollectAsync(CraftTaskId taskId, CancellationToken ct = default)
        {
            return await CollectInternalAsync(taskId, requireComplete: false, ct);
        }

        private async UniTask<CraftCollectResult> CollectInternalAsync(
            CraftTaskId taskId,
            bool requireComplete,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (taskId.IsEmpty)
                return CraftCollectResult.Fail(CraftingError.TaskNotFound);

            var taskSaveData = await FindSavedTaskAsync(taskId, ct);
            if (taskSaveData == null)
                return CraftCollectResult.Fail(CraftingError.TaskNotFound);

            var data = await _configProvider.LoadAsync(ct);
            if (!data.RecipesById.TryGetValue(taskSaveData.RecipeId, out var recipe))
                return CraftCollectResult.Fail(CraftingError.RecipeNotFound);

            var task = ToDomainTask(taskSaveData, recipe);
            if (requireComplete && !task.IsComplete(_clock.UtcNow))
                return CraftCollectResult.Fail(CraftingError.TaskNotReady);

            try
            {
                await _rewardApplier.ApplyAsync(task.Recipe.OutputItemId, task.Recipe.OutputCount, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return CraftCollectResult.Fail(CraftingError.InventoryOperationFailed);
            }

            await RemoveSavedTaskAsync(taskId, ct);

            return CraftCollectResult.Ok(task.Recipe.OutputItemId, task.Recipe.OutputCount);
        }

        private async UniTask<int> GetActiveTaskCountAsync(string stationId, CancellationToken ct)
        {
            var activeTasks = await GetActiveTasksAsync(stationId, ct);
            return activeTasks.Count;
        }

        private static int GetStationSlotLimit(string stationId)
        {
            return string.Equals(stationId, CraftingStationIds.LureCrafting, StringComparison.Ordinal)
                ? DefaultStationSlotLimit
                : DefaultStationSlotLimit;
        }

        private async UniTask<CraftingModuleSaveData> GetSaveDataAsync(CancellationToken ct)
        {
            return await _saveService.GetReadonlyModuleAsync(data => data.Crafting, ct) ?? new CraftingModuleSaveData();
        }

        private async UniTask<CraftTaskSaveData> FindSavedTaskAsync(CraftTaskId taskId, CancellationToken ct)
        {
            var saveData = await GetSaveDataAsync(ct);
            return saveData.Tasks?.FirstOrDefault(task =>
                task != null &&
                string.Equals(task.TaskId, taskId.Value, StringComparison.Ordinal));
        }

        private async UniTask RemoveSavedTaskAsync(CraftTaskId taskId, CancellationToken ct)
        {
            await _saveService.UpdateModuleAsync(data => data.Crafting, crafting =>
            {
                crafting.Tasks ??= new List<CraftTaskSaveData>();
                crafting.Tasks.RemoveAll(task =>
                    task == null ||
                    string.Equals(task.TaskId, taskId.Value, StringComparison.Ordinal));
            }, ct);
        }

        private static CraftTaskSaveData ToSaveData(CraftTask task)
        {
            return new CraftTaskSaveData
            {
                TaskId = task.Id.Value,
                RecipeId = task.Recipe.Id,
                StationId = task.Recipe.StationId,
                StartedAtUnixSeconds = task.StartedAtUtc.ToUnixTimeSeconds(),
                CompleteAtUnixSeconds = task.CompleteAtUtc.ToUnixTimeSeconds()
            };
        }

        private static CraftTask ToDomainTask(CraftTaskSaveData taskSaveData, CraftingRecipeConfig recipe)
        {
            return new CraftTask(
                new CraftTaskId(taskSaveData.TaskId),
                recipe,
                DateTimeOffset.FromUnixTimeSeconds(Math.Max(0, taskSaveData.StartedAtUnixSeconds)),
                DateTimeOffset.FromUnixTimeSeconds(Math.Max(taskSaveData.StartedAtUnixSeconds, taskSaveData.CompleteAtUnixSeconds)));
        }
    }
}
