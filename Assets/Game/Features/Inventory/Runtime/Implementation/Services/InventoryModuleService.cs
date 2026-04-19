using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using Inventory.Implementation.Core;
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
        private readonly Subject<InventoryChangedEvent> _inventoryChangedSubject = new();
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private readonly HashSet<string> _loadedOwners = new(StringComparer.Ordinal);
        private readonly List<IInventoryItemUseHandler> _inventoryItemUseHandlers;
        
        public Observable<InventoryChangedEvent> OnInventoryChanged => _inventoryChangedSubject;
        
        public InventoryModuleService(IInventoryServerApi inventoryServerApi, IPlayerIdentityProvider playerIdentityProvider)
        {
            _inventoryServerApi = inventoryServerApi ?? throw new ArgumentNullException(nameof(inventoryServerApi));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));

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
            if (response.Inventory == null)
            {
                throw new InvalidOperationException("Inventory remove response does not contain inventory snapshot.");
            }

            await ApplySnapshotAsync(response.Inventory, cancellationToken);
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
            if (response.Inventory == null)
            {
                throw new InvalidOperationException("Inventory remove-batch response does not contain inventory snapshot.");
            }

            await ApplySnapshotAsync(response.Inventory, cancellationToken);
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
            cancellationToken.ThrowIfCancellationRequested();
            var resolvedOwnerId = ResolveCurrentOwnerId();
            if (_loadedOwners.Contains(resolvedOwnerId))
            {
                return;
            }

            await _loadSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_loadedOwners.Contains(resolvedOwnerId))
                {
                    return;
                }

                var response = await _inventoryServerApi.LoadAsync(new InventoryLoadCommand
                {
                    PlayerId = resolvedOwnerId
                }, cancellationToken);
                EnsureServerSuccess(response, "load");

                var snapshotItems = MapSnapshotItems(resolvedOwnerId, response.Inventory);
                await ApplySnapshotAsync(snapshotItems, cancellationToken);
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
                    string.IsNullOrWhiteSpace(item.CategoryId) ||
                    item.Amount <= 0)
                {
                    continue;
                }

                mapped.Add(new InventoryItemView(ownerId, item.ItemId, item.Amount, item.CategoryId));
            }

            return mapped;
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
