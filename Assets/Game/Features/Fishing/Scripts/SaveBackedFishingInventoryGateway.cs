using System;
using System.Collections.Generic;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
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
}
