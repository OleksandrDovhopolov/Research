using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Fishing
{
    public sealed class SaveBackedFishingInventoryGateway : IFishingInventoryGateway
    {
        private const string RegularCategoryId = "regular";

        private readonly SaveService _saveService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IInventorySnapshotService _inventorySnapshotService;

        public SaveBackedFishingInventoryGateway(
            SaveService saveService,
            IPlayerIdentityProvider playerIdentityProvider,
            IInventorySnapshotService inventorySnapshotService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _inventorySnapshotService = inventorySnapshotService ?? throw new ArgumentNullException(nameof(inventorySnapshotService));
        }

        public async UniTask<bool> HasItemAsync(string itemId, int amount, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            var inventory = await _saveService.GetReadonlyModuleAsync(data => data.Inventory, ct);
            return GetAmount(inventory, itemId) >= amount;
        }

        public async UniTask<bool> RemoveItemAsync(string itemId, int amount, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            var removed = false;
            await _saveService.UpdateModuleAsync(data => data.Inventory, inventory =>
            {
                var current = GetAmount(inventory, itemId);
                if (current < amount)
                    return;

                SetAmount(inventory, itemId, current - amount);
                removed = true;
            }, ct);

            if (removed)
                await ApplySnapshotAsync(ct);

            return removed;
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
            var ownerId = ResolveOwnerId();
            var inventory = await _saveService.GetReadonlyModuleAsync(data => data.Inventory, ct);
            var items = ReadAllItems(inventory, ownerId);
            await _inventorySnapshotService.ApplySnapshotAsync(items, ct);
        }

        private string ResolveOwnerId()
        {
            var ownerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new InvalidOperationException("Player id is empty.");

            return ownerId;
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

            if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out var parsed))
                return Math.Max(0, parsed);

            if (token.Type != JTokenType.Object)
                return 0;

            var amountToken = token["amount"] ?? token["Amount"] ?? token["stackCount"] ?? token["StackCount"];
            if (amountToken == null || amountToken.Type == JTokenType.Null)
                return 0;

            if (amountToken.Type == JTokenType.Integer)
                return Math.Max(0, amountToken.Value<int>());

            return amountToken.Type == JTokenType.String && int.TryParse(amountToken.Value<string>(), out parsed)
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
                if (amount <= 0)
                    continue;

                result.Add(new InventoryItemView(ownerId, property.Name, amount, RegularCategoryId));
            }

            return result;
        }
    }

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
