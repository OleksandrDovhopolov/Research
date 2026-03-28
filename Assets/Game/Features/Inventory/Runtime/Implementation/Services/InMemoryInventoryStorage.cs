using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;
using UnityEngine;

namespace Inventory.Implementation.Services
{
    public sealed class InMemoryInventoryStorage : IInventoryStorage
    {
        private readonly Dictionary<string, List<InventoryItemView>> _storage = new();
        private readonly AtomicJsonFileSaver _jsonFileSaver = new();
        private static readonly Regex InvalidFileNameChars = new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);
        private string _rootPath;

        public async UniTask<IReadOnlyList<InventoryItemView>> LoadAsync(
            string ownerId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateOwnerId(ownerId);
            await EnsureInitializedAsync(cancellationToken);

            if (_storage.TryGetValue(ownerId, out var savedItems))
            {
                return savedItems.ToArray();
            }

            var loadedItems = await _jsonFileSaver.LoadAsync<List<InventoryItemView>>(
                GetFilePath(ownerId),
                cancellationToken,
                $"InMemoryInventoryStorage Load failed for owner {ownerId}") ?? new List<InventoryItemView>();

            _storage[ownerId] = loadedItems;
            return loadedItems.ToArray();
        }

        public async UniTask SaveAsync(
            string ownerId,
            IReadOnlyList<InventoryItemView> items,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateOwnerId(ownerId);
            await EnsureInitializedAsync(cancellationToken);

            _storage[ownerId] = new List<InventoryItemView>(items);
            await _jsonFileSaver.SaveAsync(
                GetFilePath(ownerId),
                _storage[ownerId],
                cancellationToken,
                $"InMemoryInventoryStorage Save failed for owner {ownerId}");
        }

        private UniTask EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            _rootPath ??= Path.Combine(Application.persistentDataPath, "inventory");
            return _jsonFileSaver.InitializeAsync(_rootPath, cancellationToken, "InMemoryInventoryStorage");
        }

        private string GetFilePath(string ownerId)
        {
            var sanitizedOwnerId = InvalidFileNameChars.Replace(ownerId, "_");
            return Path.Combine(_rootPath, $"owner_{sanitizedOwnerId}.json");
        }

        private static void ValidateOwnerId(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                throw new System.ArgumentException("Owner ID cannot be null or empty", nameof(ownerId));
            }
        }
    }
}
