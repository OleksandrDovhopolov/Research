using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Newtonsoft.Json.Linq;
using VContainer;

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

    public sealed class SaveBackedCraftingInventoryGateway : ICraftingInventoryGateway
    {
        private const string RegularCategoryId = "regular";

        private readonly SaveService _saveService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IInventorySnapshotService _inventorySnapshotService;

        public SaveBackedCraftingInventoryGateway(
            SaveService saveService,
            IPlayerIdentityProvider playerIdentityProvider,
            IInventorySnapshotService inventorySnapshotService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _inventorySnapshotService = inventorySnapshotService ?? throw new ArgumentNullException(nameof(inventorySnapshotService));
        }

        public async UniTask AddItemAsync(string itemId, int amount, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                throw new ArgumentException("Item id and amount must be valid.");

            await _saveService.UpdateModuleAsync(data => data.Inventory, inventory =>
            {
                var current = GetAmount(inventory, itemId);
                SetAmount(inventory, itemId, current + amount);
            }, ct);

            await ApplySnapshotAsync(ct);
        }

        private async UniTask ApplySnapshotAsync(CancellationToken ct)
        {
            var ownerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new InvalidOperationException("Player id is empty.");

            var inventory = await _saveService.GetReadonlyModuleAsync(data => data.Inventory, ct);
            var items = ReadAllItems(inventory, ownerId);
            await _inventorySnapshotService.ApplySnapshotAsync(items, ct);
        }

        private static int GetAmount(InventoryModuleSaveData inventory, string itemId)
        {
            if (inventory?.InventoryItems is not JObject items || string.IsNullOrWhiteSpace(itemId))
                return 0;

            var token = items[itemId];
            if (token == null || token.Type == JTokenType.Null)
                return 0;

            if (token.Type == JTokenType.Integer)
                return Math.Max(0, token.Value<int>());

            return token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out var parsed)
                ? Math.Max(0, parsed)
                : 0;
        }

        private static void SetAmount(InventoryModuleSaveData inventory, string itemId, int amount)
        {
            inventory.InventoryItems ??= new JObject();
            if (inventory.InventoryItems is not JObject items)
            {
                items = new JObject();
                inventory.InventoryItems = items;
            }

            if (amount <= 0)
                items.Remove(itemId);
            else
                items[itemId] = amount;
        }

        private static IReadOnlyList<InventoryItemView> ReadAllItems(InventoryModuleSaveData inventory, string ownerId)
        {
            if (inventory?.InventoryItems is not JObject items || !items.HasValues)
                return Array.Empty<InventoryItemView>();

            var result = new List<InventoryItemView>(items.Count);
            foreach (var property in items.Properties())
            {
                var amount = GetAmount(inventory, property.Name);
                if (amount > 0)
                    result.Add(new InventoryItemView(ownerId, property.Name, amount, RegularCategoryId));
            }

            return result;
        }
    }

    public static class CraftingVContainerBindings
    {
        public static void RegisterCrafting(this IContainerBuilder builder)
        {
            builder.Register<CraftingConfigValidator>(Lifetime.Singleton);
            builder.Register<ICraftingConfigContentSource, StreamingAssetsCraftingConfigContentSource>(Lifetime.Singleton);
            builder.Register<ICraftingConfigProvider, JsonCraftingConfigProvider>(Lifetime.Singleton);
            builder.Register<ICraftingClock, SystemCraftingClock>(Lifetime.Singleton);
            builder.Register<ICraftingInventoryGateway, SaveBackedCraftingInventoryGateway>(Lifetime.Singleton);
            builder.Register<ICraftingService, CraftingService>(Lifetime.Singleton);
        }
    }
}
