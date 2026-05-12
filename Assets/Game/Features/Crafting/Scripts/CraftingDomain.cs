using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Crafting
{
    public static class CraftingStationIds
    {
        public const string LureCrafting = "lure_crafting_station";
    }

    public enum CraftingError
    {
        None = 0,
        RecipeNotFound,
        RecipeDisabled,
        StationQueueFull,
        TaskNotFound,
        TaskNotReady,
        InventoryOperationFailed,
        ConfigInvalid
    }

    public readonly struct CraftTaskId
    {
        public CraftTaskId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }
        public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
        public override string ToString() => Value;
    }

    public sealed class CraftTask
    {
        public CraftTask(CraftTaskId id, CraftingRecipeConfig recipe, DateTimeOffset startedAtUtc, DateTimeOffset completeAtUtc)
        {
            Id = id;
            Recipe = recipe;
            StartedAtUtc = startedAtUtc;
            CompleteAtUtc = completeAtUtc;
        }

        public CraftTaskId Id { get; }
        public CraftingRecipeConfig Recipe { get; }
        public DateTimeOffset StartedAtUtc { get; }
        public DateTimeOffset CompleteAtUtc { get; }
        public bool IsComplete(DateTimeOffset nowUtc) => nowUtc >= CompleteAtUtc;
    }

    public sealed class CraftStartResult
    {
        public bool Success { get; private set; }
        public CraftingError Error { get; private set; }
        public CraftTaskId TaskId { get; private set; }
        public CraftingRecipeConfig Recipe { get; private set; }
        public DateTimeOffset CompleteAtUtc { get; private set; }

        public static CraftStartResult Ok(CraftTask task)
        {
            return new CraftStartResult
            {
                Success = true,
                Error = CraftingError.None,
                TaskId = task.Id,
                Recipe = task.Recipe,
                CompleteAtUtc = task.CompleteAtUtc
            };
        }

        public static CraftStartResult Fail(CraftingError error)
        {
            return new CraftStartResult
            {
                Success = false,
                Error = error,
                TaskId = new CraftTaskId(string.Empty)
            };
        }
    }

    public sealed class CraftCollectResult
    {
        public bool Success { get; private set; }
        public CraftingError Error { get; private set; }
        public string OutputItemId { get; private set; }
        public int OutputCount { get; private set; }

        public static CraftCollectResult Ok(string outputItemId, int outputCount)
        {
            return new CraftCollectResult
            {
                Success = true,
                Error = CraftingError.None,
                OutputItemId = outputItemId,
                OutputCount = outputCount
            };
        }

        public static CraftCollectResult Fail(CraftingError error)
        {
            return new CraftCollectResult
            {
                Success = false,
                Error = error
            };
        }
    }

    public interface ICraftingService
    {
        UniTask<CraftStartResult> StartCraftAsync(string recipeId, CancellationToken ct = default);
        UniTask<IReadOnlyList<CraftTask>> GetActiveTasksAsync(string stationId, CancellationToken ct = default);
        UniTask<CraftTask> GetFirstActiveTaskAsync(string stationId, CancellationToken ct = default);
        UniTask<CraftCollectResult> CollectAsync(CraftTaskId taskId, CancellationToken ct = default);
        UniTask<CraftCollectResult> CompleteAndCollectAsync(CraftTaskId taskId, CancellationToken ct = default);
    }

    public interface ICraftingRewardApplier
    {
        UniTask ApplyAsync(string outputItemId, int outputCount, CancellationToken ct = default);
    }
}
