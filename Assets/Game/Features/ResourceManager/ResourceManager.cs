using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Resources.Core
{
    public class ResourceManager
    {
        public delegate void ResourceAmountChangedHandler(ResourceType type, int newAmount);
        public event ResourceAmountChangedHandler ResourceAmountChanged;

        private readonly Dictionary<ResourceType, int> _amountByType = new()
        {
            { ResourceType.Gold, 0 },
            { ResourceType.Energy, 0 },
            { ResourceType.Gems, 0 },
        };
        private readonly JsonResourcesStorage _storage;
        private readonly CancellationTokenSource _saveCts = new();
        private bool _isInitialized;
        private bool _isDisposed;

        public ResourceManager() : this(new JsonResourcesStorage())
        {
        }

        public ResourceManager(JsonResourcesStorage storage)
        {
            _storage = storage;
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            ThrowIfDisposed();
            if (_isInitialized)
            {
                return;
            }

            await _storage.InitializeAsync(ct);
            var saveData = await _storage.LoadAsync(ct);
            ApplySaveData(saveData);
            _isInitialized = true;
        }

        public UniTask SaveAsync(CancellationToken ct)
        {
            ThrowIfDisposed();
            return _storage.SaveAsync(CreateSaveData(), ct);
        }

        public void Add(ResourceType type, int amount)
        {
            ThrowIfDisposed();
            if (amount <= 0)
            {
                return;
            }

            _amountByType[type] += amount;
            ResourceAmountChanged?.Invoke(type, _amountByType[type]);
            QueueSave();
        }

        public bool Remove(ResourceType type, int amount)
        {
            ThrowIfDisposed();
            if (amount <= 0)
            {
                return false;
            }

            var currentAmount = _amountByType[type];
            if (currentAmount < amount)
            {
                return false;
            }

            _amountByType[type] = currentAmount - amount;
            ResourceAmountChanged?.Invoke(type, _amountByType[type]);
            QueueSave();
            return true;
        }

        public int Get(ResourceType type)
        {
            return _amountByType[type];
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _saveCts.Cancel();
            _saveCts.Dispose();
            _storage.Dispose();
        }

        private void ApplySaveData(ResourcesSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            _amountByType[ResourceType.Gold] = Mathf.Max(0, saveData.Gold);
            _amountByType[ResourceType.Energy] = Mathf.Max(0, saveData.Energy);
            _amountByType[ResourceType.Gems] = Mathf.Max(0, saveData.Gems);
        }

        private ResourcesSaveData CreateSaveData()
        {
            return new ResourcesSaveData
            {
                Gold = _amountByType[ResourceType.Gold],
                Energy = _amountByType[ResourceType.Energy],
                Gems = _amountByType[ResourceType.Gems],
            };
        }

        private void QueueSave()
        {
            if (!_isInitialized || _isDisposed)
            {
                return;
            }

            SaveSilentlyAsync(_saveCts.Token).Forget();
        }

        private async UniTaskVoid SaveSilentlyAsync(CancellationToken ct)
        {
            try
            {
                await SaveAsync(ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResourceManager] Autosave failed: {e}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ResourceManager));
            }
        }
    }
}
