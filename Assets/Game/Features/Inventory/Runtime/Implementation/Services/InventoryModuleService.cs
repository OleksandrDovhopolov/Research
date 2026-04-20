using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Inventory.Implementation.Core;
using Newtonsoft.Json.Linq;
using Rewards;
using R3;
using UnityEngine;

namespace Inventory.Implementation.Services
{
    public static class InventoryBuiltInCategoryIds
    {
        public const string Regular = "regular";
        public const string CardPack = "card_pack";
    }
    
    public sealed class InventoryModuleService : IInventoryService, IInventoryReadService, IInventoryItemUseService, IInventoryUseHandlerStorage, IInventorySnapshotService, IDisposable
    {
        private const string RemoveReason = "inventory_consume";
        private const string RemoveBatchReason = "inventory_remove_batch";

        private readonly InventoryQuerySystem _querySystem;
        private readonly InventoryWorld _world;
        private readonly IInventoryServerApi _inventoryServerApi;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly SaveService _saveService;
        private readonly IInventoryItemCategoryResolver _itemCategoryResolver;
        private readonly Subject<InventoryChangedEvent> _inventoryChangedSubject = new();
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private readonly HashSet<string> _loadedOwners = new(StringComparer.Ordinal);
        private readonly List<IInventoryItemUseHandler> _inventoryItemUseHandlers;
        private bool _isInitialized;
        
        public Observable<InventoryChangedEvent> OnInventoryChanged => _inventoryChangedSubject;
        
        public InventoryModuleService(
            IInventoryServerApi inventoryServerApi,
            IPlayerIdentityProvider playerIdentityProvider,
            SaveService saveService,
            IInventoryItemCategoryResolver itemCategoryResolver)
        {
            _inventoryServerApi = inventoryServerApi ?? throw new ArgumentNullException(nameof(inventoryServerApi));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _itemCategoryResolver = itemCategoryResolver ?? throw new ArgumentNullException(nameof(itemCategoryResolver));

            _world = new InventoryWorld();
            _querySystem = new InventoryQuerySystem(_world);

            _inventoryItemUseHandlers =  new List<IInventoryItemUseHandler>();
        }
        
        public void AddUseHandler(IInventoryItemUseHandler handler)
        {
            if (handler != null)
            {
                _inventoryItemUseHandlers.Add(handler);
            }
        }
        
        public void RemoveUseHandler(IInventoryItemUseHandler handler)
        {
            if (handler != null)
            {
                _inventoryItemUseHandlers.Remove(handler);
            }
        }
        
        public async UniTask AddItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException("Inventory add operation is server-authoritative and not supported on client.");
        }

        public async UniTask RemoveItemAsync(InventoryItemDelta itemDelta, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ownerId = ResolveCurrentOwnerId();
            await EnsureOwnerLoadedAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(itemDelta.ItemId) || itemDelta.Amount <= 0)
            {
                throw new ArgumentException("ItemId and Amount must be valid for remove command.", nameof(itemDelta));
            }

            var response = await _inventoryServerApi.RemoveAsync(new RemoveInventoryItemCommand
            {
                PlayerId = ownerId,
                ItemId = itemDelta.ItemId,
                Amount = itemDelta.Amount,
                Reason = RemoveReason
            }, cancellationToken);

            EnsureServerSuccess(response, "remove");
            await ApplyResponseSnapshotAsync(response, "remove", cancellationToken);
        }
        
        public async UniTask<InventoryBatchRemoveResult> RemoveItemsAsync(
            IReadOnlyList<InventoryItemDelta> itemDeltas,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (itemDeltas == null || itemDeltas.Count == 0)
            {
                return new InventoryBatchRemoveResult(0, 0, Array.Empty<InventoryItemDelta>());
            }

            var ownerId = ResolveCurrentOwnerId();
            await EnsureOwnerLoadedAsync(cancellationToken);

            var commandItems = new List<RemoveInventoryBatchItem>(itemDeltas.Count);
            var requestedStacks = 0;
            for (var i = 0; i < itemDeltas.Count; i++)
            {
                var itemDelta = itemDeltas[i];
                if (string.IsNullOrWhiteSpace(itemDelta.ItemId) || itemDelta.Amount <= 0)
                {
                    continue;
                }

                commandItems.Add(new RemoveInventoryBatchItem
                {
                    ItemId = itemDelta.ItemId,
                    Amount = itemDelta.Amount
                });
                requestedStacks += itemDelta.Amount;
            }

            if (commandItems.Count == 0)
            {
                return new InventoryBatchRemoveResult(0, 0, Array.Empty<InventoryItemDelta>());
            }

            var response = await _inventoryServerApi.RemoveBatchAsync(new RemoveInventoryBatchCommand
            {
                PlayerId = ownerId,
                Items = commandItems,
                Reason = RemoveBatchReason
            }, cancellationToken);

            EnsureServerSuccess(response, "remove-batch");
            await ApplyResponseSnapshotAsync(response, "remove-batch", cancellationToken);
            return new InventoryBatchRemoveResult(requestedStacks, requestedStacks, Array.Empty<InventoryItemDelta>());
        }

        public async UniTask<IReadOnlyList<InventoryItemView>> GetItemsAsync(string ownerId, string categoryId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resolvedOwnerId = ResolveCurrentOwnerId();
            await EnsureOwnerLoadedAsync(cancellationToken);
            var items = _querySystem.Execute(resolvedOwnerId, categoryId);
            WarnOnCategoryIdMismatch(resolvedOwnerId, categoryId, items);
            return items;
        }
        
        public async UniTask ConsumeItemAsync(InventoryItemDelta item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ownerId = ResolveCurrentOwnerId();
            await EnsureOwnerLoadedAsync(cancellationToken);

            var handler = _inventoryItemUseHandlers.FirstOrDefault(x => x.CanHandle(item));
            if (handler == null)
            {
                Debug.LogError($"No handler found for item type: {item.ItemId} with category: {item.CategoryId}");
                return;
            }

            if (!HasEnoughItems(ownerId, item))
            {
                Debug.LogError($"Not enough items for item type: {item.ItemId} with category: {item.CategoryId}. item.OwnerId {ownerId}");
                return;
            }

            await RemoveItemAsync(item, cancellationToken);
            
            try
            {
                await handler.UseAsync(item, ownerId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to use item {item.ItemId}. Inventory rollback is not supported in server-authoritative mode. Error: {ex.Message}");
                throw;
            }
        }

        private bool HasEnoughItems(string ownerId, InventoryItemDelta item)
        {
            var items = _querySystem.Execute(ownerId, item.CategoryId);
            return items.Any(x => x.ItemId == item.ItemId && x.StackCount >= item.Amount);
        }

        internal async UniTask EnsureOwnerLoadedAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync(cancellationToken);
        }

        internal async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ownerId = ResolveCurrentOwnerId();
            if (_isInitialized && _loadedOwners.Contains(ownerId))
            {
                return;
            }

            await _loadSemaphore.WaitAsync(cancellationToken);
            try
            {
                ownerId = ResolveCurrentOwnerId();
                if (_isInitialized && _loadedOwners.Contains(ownerId))
                {
                    return;
                }

                await _saveService.LoadAllAsync(cancellationToken);
                var inventorySaveData = await _saveService.GetReadonlyModuleAsync(data => data.Inventory, cancellationToken);
                var snapshotItems = MapSnapshotItemsFromSave(ownerId, inventorySaveData);
                await ApplySnapshotAsync(snapshotItems, cancellationToken);
                _isInitialized = true;
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        public async UniTask ApplySnapshotAsync(InventorySnapshotDto snapshot, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ownerId = ResolveCurrentOwnerId();
            var mapped = MapSnapshotItems(ownerId, snapshot);
            await ApplySnapshotAsync(mapped, cancellationToken);
        }

        public UniTask ApplySnapshotAsync(IReadOnlyList<InventoryItemView> items, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ownerId = ResolveCurrentOwnerId();
            _world.ReplaceOwnerSnapshot(ownerId, items ?? Array.Empty<InventoryItemView>());
            _loadedOwners.Add(ownerId);
            _isInitialized = true;
            PublishChanged(ownerId);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _inventoryChangedSubject?.OnCompleted();
            _inventoryChangedSubject?.Dispose();
            _loadSemaphore.Dispose();
        }

        private IReadOnlyList<InventoryItemView> MapSnapshotItems(string ownerId, InventorySnapshotDto snapshot)
        {
            if (snapshot?.Items == null || snapshot.Items.Count == 0)
            {
                return Array.Empty<InventoryItemView>();
            }

            var mapped = new List<InventoryItemView>(snapshot.Items.Count);
            for (var i = 0; i < snapshot.Items.Count; i++)
            {
                var item = snapshot.Items[i];
                if (item == null ||
                    string.IsNullOrWhiteSpace(item.ItemId) ||
                    item.Amount <= 0)
                {
                    continue;
                }

                mapped.Add(new InventoryItemView(
                    ownerId,
                    item.ItemId,
                    item.Amount,
                    ResolveCategoryId(item.ItemId, item.CategoryId)));
            }

            return mapped;
        }

        private IReadOnlyList<InventoryItemView> MapSnapshotItemsFromSave(string ownerId, InventoryModuleSaveData inventorySaveData)
        {
            if (inventorySaveData == null)
            {
                return Array.Empty<InventoryItemView>();
            }

            if (inventorySaveData.InventoryItems is JObject inventoryItemsObject && inventoryItemsObject.HasValues)
            {
                var mapped = new List<InventoryItemView>(inventoryItemsObject.Count);
                foreach (var property in inventoryItemsObject.Properties())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) ||
                        !TryExtractSnapshotAmount(property.Value, out var amount) ||
                        amount <= 0)
                    {
                        continue;
                    }

                    mapped.Add(new InventoryItemView(
                        ownerId,
                        property.Name,
                        amount,
                        ResolveCategoryId(property.Name, null)));
                }

                return mapped;
            }

            if (inventorySaveData.Owners == null || inventorySaveData.Owners.Count == 0)
            {
                return Array.Empty<InventoryItemView>();
            }

            var ownerData = inventorySaveData.Owners.FirstOrDefault(x =>
                x != null && string.Equals(x.OwnerId, ownerId, StringComparison.Ordinal));
            if (ownerData?.Items == null || ownerData.Items.Count == 0)
            {
                return Array.Empty<InventoryItemView>();
            }

            var legacyMapped = new List<InventoryItemView>(ownerData.Items.Count);
            for (var i = 0; i < ownerData.Items.Count; i++)
            {
                var item = ownerData.Items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || item.StackCount <= 0)
                {
                    continue;
                }

                legacyMapped.Add(new InventoryItemView(
                    ownerId,
                    item.ItemId,
                    item.StackCount,
                    ResolveCategoryId(item.ItemId, item.CategoryId)));
            }

            return legacyMapped;
        }

        private string ResolveCategoryId(string itemId, string preferredCategoryId)
        {
            if (!string.IsNullOrWhiteSpace(preferredCategoryId))
            {
                return preferredCategoryId;
            }

            var resolved = _itemCategoryResolver.ResolveCategoryId(itemId);
            return string.IsNullOrWhiteSpace(resolved)
                ? InventoryBuiltInCategoryIds.Regular
                : resolved;
        }

        private static bool TryExtractSnapshotAmount(JToken token, out int amount)
        {
            amount = 0;
            if (token == null || token.Type == JTokenType.Null)
            {
                return false;
            }

            if (token.Type == JTokenType.Integer)
            {
                amount = token.Value<int>();
                return true;
            }

            if (token.Type == JTokenType.String)
            {
                return int.TryParse(token.Value<string>(), out amount);
            }

            if (token.Type != JTokenType.Object)
            {
                return false;
            }

            var amountToken = token["amount"] ?? token["Amount"] ?? token["stackCount"] ?? token["StackCount"];
            if (amountToken == null || amountToken.Type == JTokenType.Null)
            {
                return false;
            }

            if (amountToken.Type == JTokenType.Integer)
            {
                amount = amountToken.Value<int>();
                return true;
            }

            if (amountToken.Type == JTokenType.String)
            {
                return int.TryParse(amountToken.Value<string>(), out amount);
            }

            return false;
        }

        private void PublishChanged(string ownerId)
        {
            var allItems = _querySystem.ExecuteAll(ownerId);
            var itemsByCategory = allItems
                .GroupBy(item => item.CategoryId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<InventoryItemView>)group.ToList());

            _inventoryChangedSubject.OnNext(new InventoryChangedEvent(
                ownerId,
                itemsByCategory,
                DateTime.UtcNow));
        }

        private string ResolveCurrentOwnerId()
        {
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            return playerId;
        }

        private static void EnsureServerSuccess(InventoryOperationResponse response, string operationName)
        {
            if (response == null)
            {
                throw new InvalidOperationException($"Inventory {operationName} response is null.");
            }

            if (!response.Success)
            {
                throw new InvalidOperationException(
                    $"Inventory {operationName} rejected. Code={response.ErrorCode ?? "<none>"}, Message={response.ErrorMessage ?? "<none>"}");
            }
        }

        private async UniTask ApplyResponseSnapshotAsync(
            InventoryOperationResponse response,
            string operationName,
            CancellationToken cancellationToken)
        {
            if (response?.PlayerState?.InventoryItems == null)
            {
                throw new InvalidOperationException(
                    $"Inventory {operationName} response does not contain playerState.inventoryItems.");
            }

            var snapshot = new InventorySnapshotDto
            {
                Items = response.PlayerState.InventoryItems
                    .Where(item => item != null)
                    .Select(item => new InventorySnapshotItemDto
                    {
                        ItemId = item.ItemId,
                        Amount = item.Amount
                    })
                    .ToList()
            };

            await ApplySnapshotAsync(snapshot, cancellationToken);
        }
        
        #region CategoryMismatchHandler
        
        private void WarnOnCategoryIdMismatch(string ownerId, string requestedCategoryId, IReadOnlyList<InventoryItemView> requestedItems)
        {
            if (requestedItems.Count > 0 || string.IsNullOrWhiteSpace(requestedCategoryId))
            {
                return;
            }

            var allItems = _querySystem.ExecuteAll(ownerId);
            if (allItems.Count == 0)
            {
                return;
            }

            var existingCategoryIds = allItems
                .Select(item => item.CategoryId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var normalizedRequested = NormalizeCategoryId(requestedCategoryId);
            var closeMatches = existingCategoryIds
                .Where(existingId => NormalizeCategoryId(existingId) == normalizedRequested)
                .ToList();

            if (closeMatches.Count == 0)
            {
                return;
            }

            Debug.LogWarning(
                $"[InventoryModuleService] Category mismatch detected for owner '{ownerId}'. " +
                $"Requested categoryId='{requestedCategoryId}', but found similar stored categoryId(s): [{string.Join(", ", closeMatches)}]. " +
                "Check ItemCategory asset IDs vs constants (example: 'CardPack' vs 'card_pack').");
        }

        private static string NormalizeCategoryId(string categoryId)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                return string.Empty;
            }

            return new string(categoryId
                .Where(ch => ch != '_' && ch != '-' && ch != ' ')
                .Select(char.ToLowerInvariant)
                .ToArray());
        }
        
        #endregion
    }
}
