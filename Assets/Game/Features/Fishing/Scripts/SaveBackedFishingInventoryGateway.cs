using System;
using System.Collections.Generic;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Fishing
{
    public sealed class SaveBackedFishingInventoryGateway : IFishingInventoryGateway
    {
        private const string RegularCategoryId = "regular";

        private readonly SaveService _saveService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IInventorySnapshotService _inventorySnapshotService;
        private readonly IInventoryReadService _inventoryReadService;
        private readonly IInventoryService _inventoryService;

        public SaveBackedFishingInventoryGateway(
            SaveService saveService,
            IPlayerIdentityProvider playerIdentityProvider,
            IInventorySnapshotService inventorySnapshotService,
            IInventoryReadService inventoryReadService,
            IInventoryService inventoryService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _inventorySnapshotService = inventorySnapshotService ?? throw new ArgumentNullException(nameof(inventorySnapshotService));
            _inventoryReadService = inventoryReadService ?? throw new ArgumentNullException(nameof(inventoryReadService));
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        }

        public async UniTask<bool> HasItemAsync(string itemId, int amount, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            var ownerId = ResolveOwnerId();
            var categoryId = ResolveCategoryId();
            var inventoryItems = await _inventoryReadService.GetItemsAsync(ownerId, categoryId, ct);
            return GetRuntimeAmount(inventoryItems, itemId) >= amount;
        }

        public async UniTask<bool> RemoveItemAsync(string itemId, int amount, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            try
            {
                var ownerId = ResolveOwnerId();
                var categoryId = ResolveCategoryId();
                await _inventoryService.RemoveItemAsync(new InventoryItemDelta(ownerId, itemId, amount, categoryId), ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[FishingInventoryGateway] Failed to remove item '{itemId}' x{amount}. {exception}");
                return false;
            }
        }

        public async UniTask AddItemAsync(string itemId, int amount, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                throw new ArgumentException("Item id and amount must be valid.");

            var ownerId = ResolveOwnerId();
            var categoryId = ResolveCategoryId();
            var currentItems = await _inventoryReadService.GetItemsAsync(ownerId, categoryId, ct);

            await _saveService.UpdateModuleAsync(data => data.Inventory, inventory =>
            {
                var current = GetAmount(inventory, itemId);
                SetAmount(inventory, itemId, current + amount);
            }, ct);

            var updatedItems = AddToRuntimeSnapshot(currentItems, ownerId, itemId, amount, categoryId);
            await _inventorySnapshotService.ApplySnapshotAsync(updatedItems, ct);
        }

        private static string ResolveCategoryId()
        {
            return RegularCategoryId;
        }

        private string ResolveOwnerId()
        {
            var ownerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new InvalidOperationException("Player id is empty.");

            return ownerId;
        }

        private static int GetRuntimeAmount(IReadOnlyList<InventoryItemView> inventoryItems, string itemId)
        {
            if (inventoryItems == null || string.IsNullOrWhiteSpace(itemId))
                return 0;

            var amount = 0;
            for (var i = 0; i < inventoryItems.Count; i++)
            {
                var item = inventoryItems[i];
                if (string.Equals(item.ItemId, itemId, StringComparison.Ordinal))
                    amount += Math.Max(0, item.StackCount);
            }

            return amount;
        }

        private static IReadOnlyList<InventoryItemView> AddToRuntimeSnapshot(
            IReadOnlyList<InventoryItemView> currentItems,
            string ownerId,
            string itemId,
            int amount,
            string categoryId)
        {
            var result = new List<InventoryItemView>();
            var updatedAmount = Math.Max(0, amount);

            if (currentItems != null)
            {
                for (var i = 0; i < currentItems.Count; i++)
                {
                    var item = currentItems[i];
                    if (string.IsNullOrWhiteSpace(item.ItemId) || item.StackCount <= 0)
                        continue;

                    if (string.Equals(item.ItemId, itemId, StringComparison.Ordinal))
                    {
                        updatedAmount += Math.Max(0, item.StackCount);
                        continue;
                    }

                    result.Add(item);
                }
            }

            if (updatedAmount > 0)
                result.Add(new InventoryItemView(ownerId, itemId, updatedAmount, categoryId));

            return result;
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
    }
}
