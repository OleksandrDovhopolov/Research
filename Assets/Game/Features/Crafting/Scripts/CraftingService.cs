using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Crafting
{
    public sealed class CraftingService : ICraftingService
    {
        private const string DefaultStationId = "lure_crafting_station";
        private const int DefaultStationSlotLimit = 1;

        private readonly ICraftingConfigProvider _configProvider;
        private readonly ICraftingInventoryGateway _inventoryGateway;
        private readonly ICraftingClock _clock;
        private readonly Dictionary<string, CraftTask> _tasksById = new(StringComparer.Ordinal);

        public CraftingService(
            ICraftingConfigProvider configProvider,
            ICraftingInventoryGateway inventoryGateway,
            ICraftingClock clock)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _inventoryGateway = inventoryGateway ?? throw new ArgumentNullException(nameof(inventoryGateway));
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

            if (GetActiveTaskCount(recipe.StationId) >= GetStationSlotLimit(recipe.StationId))
                return CraftStartResult.Fail(CraftingError.StationQueueFull);

            var now = _clock.UtcNow;
            var task = new CraftTask(
                new CraftTaskId(Guid.NewGuid().ToString("N")),
                recipe,
                now,
                now.AddSeconds(Math.Max(0, recipe.CraftTimeSeconds)));

            _tasksById[task.Id.Value] = task;
            return CraftStartResult.Ok(task);
        }

        public async UniTask<CraftCollectResult> CollectAsync(CraftTaskId taskId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (taskId.IsEmpty || !_tasksById.TryGetValue(taskId.Value, out var task))
                return CraftCollectResult.Fail(CraftingError.TaskNotFound);

            if (!task.IsComplete(_clock.UtcNow))
                return CraftCollectResult.Fail(CraftingError.TaskNotReady);

            await _inventoryGateway.AddItemAsync(task.Recipe.OutputItemId, task.Recipe.OutputCount, ct);
            _tasksById.Remove(taskId.Value);

            return CraftCollectResult.Ok(task.Recipe.OutputItemId, task.Recipe.OutputCount);
        }

        private int GetActiveTaskCount(string stationId)
        {
            return _tasksById.Values.Count(task =>
                task != null &&
                string.Equals(task.Recipe?.StationId, stationId, StringComparison.Ordinal));
        }

        private static int GetStationSlotLimit(string stationId)
        {
            return string.Equals(stationId, DefaultStationId, StringComparison.Ordinal)
                ? DefaultStationSlotLimit
                : DefaultStationSlotLimit;
        }
    }
}
