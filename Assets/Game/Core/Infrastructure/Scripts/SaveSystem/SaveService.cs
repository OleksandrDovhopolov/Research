using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Infrastructure
{
    public sealed class SaveService : IDisposable
    {
        private const int CurrentSchemaVersion = 2;
        private const string HashSalt = "research_save_v2_salt";
        private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(600);
        private static readonly TimeSpan SaveRateLimit = TimeSpan.FromMilliseconds(500);

        private readonly ISaveStorage _storage;
        private readonly SaveMigrationService _migrationService;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private CancellationTokenSource _debounceCts = new();

        private GameSaveData _data;
        private bool _isLoaded;
        private bool _isDirty;
        private bool _disposed;
        private DateTimeOffset _lastSaveUtc = DateTimeOffset.MinValue;

        public event Action OnBeforeSave;
        public event Action OnAfterLoad;

        public SaveService(ISaveStorage storage, SaveMigrationService migrationService)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
        }

        public async UniTask<GameSaveData> LoadAllAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            GameSaveData loadedSnapshot;
            bool shouldPersistMigration = false;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isLoaded)
                {
                    return CloneDetached(_data);
                }

                if (_storage.Exists())
                {
                    var json = await _storage.LoadAsync(cancellationToken);
                    _data = DeserializeOrDefault(json, out var usedDefault, out var deserializeReason);
                    Debug.Log(
                        $"[SaveService] LoadAllAsync storage payload processed. " +
                        $"RawLength={(json?.Length ?? 0)}, UsedDefault={usedDefault}, Reason={deserializeReason}, " +
                        $"Preview={TruncateForLog(json)}");
                }
                else
                {
                    var migrated = await _migrationService.TryMigrateLegacyAsync(CurrentSchemaVersion, cancellationToken);
                    _data = migrated ?? CreateDefaultSave();
                    shouldPersistMigration = migrated != null;
                    _isDirty = shouldPersistMigration;
                }

                EnsureDefaults(_data);
                ValidateData(_data);
                VerifyHash(_data);
                Debug.Log(
                    $"[SaveService] LoadAllAsync normalized snapshot. " +
                    $"Resources(G={_data.Resources.Gold},E={_data.Resources.Energy},Gem={_data.Resources.Gems}), " +
                    $"InventoryOwners={_data.Inventory.Owners.Count}, " +
                    $"InventoryItemsTokenType={_data.Inventory.InventoryItems?.Type}, " +
                    $"CardCollections={_data.CardCollections.Count}, EventStates={_data.EventStates.Count}, " +
                    $"SaveId={_data.Meta.SaveId}, Revision={_data.Meta.Revision}");
                _isLoaded = true;
                loadedSnapshot = CloneDetached(_data);
            }
            finally
            {
                _semaphore.Release();
            }

            OnAfterLoad?.Invoke();

            if (shouldPersistMigration)
            {
                await SaveAllAsync(cancellationToken);
                await _migrationService.CleanupLegacyFilesAsync(cancellationToken);
            }

            return loadedSnapshot;
        }

        public async UniTask SaveAllAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureLoadedAsync(cancellationToken);

            string jsonToSave = null;
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_isDirty)
                {
                    return;
                }

                var now = DateTimeOffset.UtcNow;
                var elapsed = now - _lastSaveUtc;
                if (elapsed < SaveRateLimit)
                {
                    var delay = SaveRateLimit - elapsed;
                    await UniTask.Delay(delay, cancellationToken: cancellationToken);
                }

                OnBeforeSave?.Invoke();
                _data.Meta.SchemaVersion = CurrentSchemaVersion;
                _data.Meta.LastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _data.Meta.Revision = Math.Max(0, _data.Meta.Revision) + 1;
                _data.Meta.Hash = string.Empty;
                _data.Meta.Hash = ComputeHash(_data);
                jsonToSave = JsonConvert.SerializeObject(_data, Formatting.Indented);
            }
            finally
            {
                _semaphore.Release();
            }

            await _storage.SaveAsync(jsonToSave, cancellationToken);

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _isDirty = false;
                _lastSaveUtc = DateTimeOffset.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async UniTask<TModule> GetReadonlyModuleAsync<TModule>(
            Func<GameSaveData, TModule> selector,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            await EnsureLoadedAsync(cancellationToken);
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (selector == null)
                {
                    throw new ArgumentNullException(nameof(selector));
                }

                var module = selector(_data);
                return CloneDetached(module);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async UniTask UpdateModuleAsync<TModule>(
            Func<GameSaveData, TModule> selector,
            Action<TModule> mutate,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            await EnsureLoadedAsync(cancellationToken);
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (selector == null)
                {
                    throw new ArgumentNullException(nameof(selector));
                }

                if (mutate == null)
                {
                    throw new ArgumentNullException(nameof(mutate));
                }

                var module = selector(_data);
                mutate(module);
                _isDirty = true;
            }
            finally
            {
                _semaphore.Release();
            }

            MarkDirty();
        }

        public void MarkDirty()
        {
            if (_disposed)
            {
                return;
            }

            _debounceCts.Cancel();
            _debounceCts.Dispose();
            _debounceCts = new CancellationTokenSource();
            DebouncedSaveAsync(_debounceCts.Token).Forget();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _debounceCts.Cancel();
            _debounceCts.Dispose();
            _semaphore.Dispose();
        }

        private async UniTask EnsureLoadedAsync(CancellationToken cancellationToken)
        {
            if (_isLoaded)
            {
                return;
            }

            await LoadAllAsync(cancellationToken);
        }

        private async UniTaskVoid DebouncedSaveAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(DebounceDelay, cancellationToken: cancellationToken);
                await SaveAllAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Debounced save failed: {ex}");
            }
        }

        private static GameSaveData DeserializeOrDefault(string json, out bool usedDefault, out string reason)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                usedDefault = true;
                reason = "empty-payload";
                return CreateDefaultSave();
            }

            try
            {
                var parsed = JsonConvert.DeserializeObject<GameSaveData>(json);
                if (parsed != null)
                {
                    usedDefault = false;
                    reason = "ok";
                    return parsed;
                }

                usedDefault = true;
                reason = "deserialized-null";
                return CreateDefaultSave();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Save file is corrupted or invalid. Falling back to defaults. {ex.Message}");
                usedDefault = true;
                reason = $"exception:{ex.GetType().Name}";
                return CreateDefaultSave();
            }
        }

        private static GameSaveData CreateDefaultSave()
        {
            return GameSaveData.CreateDefault(CurrentSchemaVersion, Guid.NewGuid().ToString("N"));
        }

        private static void EnsureDefaults(GameSaveData data)
        {
            data.Meta ??= new MetaData();
            data.Inventory ??= new InventoryModuleSaveData();
            data.Inventory.Owners ??= new List<InventoryOwnerSaveData>();
            data.Inventory.InventoryItems ??= new JObject();
            data.CardCollections ??= new List<CardCollectionModuleSaveData>();
            data.EventStates ??= new List<EventStateSaveData>();
            data.Resources ??= new ResourcesModuleSaveData();
            data.FortuneWheel ??= new FortuneWheelModuleSaveData();
            data.CustomModulesJson ??= new Dictionary<string, string>();
            data.Meta.SaveId ??= Guid.NewGuid().ToString("N");
            data.Meta.Hash ??= string.Empty;
        }

        private static void ValidateData(GameSaveData data)
        {
            data.Resources.Gold = Math.Max(0, data.Resources.Gold);
            data.Resources.Energy = Math.Max(0, data.Resources.Energy);
            data.Resources.Gems = Math.Max(0, data.Resources.Gems);
            data.Resources.Version = Math.Max(1, data.Resources.Version);
            data.FortuneWheel.AvailableSpins = Math.Max(0, data.FortuneWheel.AvailableSpins);
            data.FortuneWheel.UpdatedAt = Math.Max(0, data.FortuneWheel.UpdatedAt);

            foreach (var owner in data.Inventory.Owners)
            {
                owner.OwnerId = string.IsNullOrWhiteSpace(owner.OwnerId) ? "player_1" : owner.OwnerId;
                owner.Items ??= new List<InventoryItemSaveData>();
                owner.Items = owner.Items
                    .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
                    .Select(x =>
                    {
                        x.OwnerId = owner.OwnerId;
                        x.StackCount = Math.Max(0, x.StackCount);
                        return x;
                    })
                    .ToList();
            }

            if (data.Inventory.InventoryItems is not JObject inventoryItemsObject)
            {
                data.Inventory.InventoryItems = new JObject();
            }
            else
            {
                var normalizedInventoryItems = new JObject();
                foreach (var property in inventoryItemsObject.Properties())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) ||
                        !TryExtractInventoryAmount(property.Value, out var amount) ||
                        amount <= 0)
                    {
                        continue;
                    }

                    normalizedInventoryItems[property.Name] = amount;
                }

                data.Inventory.InventoryItems = normalizedInventoryItems;
            }

            data.CardCollections = data.CardCollections
                .Where(x => !string.IsNullOrWhiteSpace(x.EventId))
                .ToList();
            foreach (var collection in data.CardCollections)
            {
                collection.Points = Math.Max(0, collection.Points);
                collection.Version = Math.Max(1, collection.Version);
                collection.Cards ??= new List<CardProgressSaveData>();
                collection.Cards = collection.Cards
                    .Where(x => !string.IsNullOrWhiteSpace(x.CardId))
                    .ToList();
            }
        }

        private static void VerifyHash(GameSaveData data)
        {
            if (string.IsNullOrWhiteSpace(data.Meta.Hash))
            {
                return;
            }

            var loadedHash = data.Meta.Hash;
            var computedHash = ComputeHash(data);
            if (!string.Equals(loadedHash, computedHash, StringComparison.Ordinal))
            {
                Debug.LogWarning("[SaveService] Save hash mismatch detected. Continuing with loaded data.");
            }
        }

        private static bool TryExtractInventoryAmount(JToken token, out int amount)
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

        private static string ComputeHash(GameSaveData data)
        {
            var previous = data.Meta.Hash;
            data.Meta.Hash = string.Empty;
            var json = JsonConvert.SerializeObject(data, Formatting.None);
            data.Meta.Hash = previous;

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(json + HashSalt);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        private static T CloneDetached<T>(T source)
        {
            if (source == null)
            {
                return default;
            }

            var json = JsonConvert.SerializeObject(source, Formatting.None);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private static string TruncateForLog(string value, int maxLength = 320)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "<empty>";
            }

            var normalized = value.Replace("\r", "\\r").Replace("\n", "\\n");
            return normalized.Length <= maxLength
                ? normalized
                : normalized.Substring(0, maxLength) + "...";
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SaveService));
            }
        }
    }
}
