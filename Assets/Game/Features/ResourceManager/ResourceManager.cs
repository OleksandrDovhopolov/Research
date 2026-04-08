using System;
using System.Collections.Generic;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using VContainer.Unity;

namespace CoreResources
{
    public class ResourceManager : IStartable
    {
        public delegate void ResourceAmountChangedHandler(ResourceType type, int newAmount);

        public event ResourceAmountChangedHandler ResourceAmountChanged;

        private readonly Dictionary<ResourceType, int> _amountByType = new()
        {
            { ResourceType.Gold, 0 }, { ResourceType.Energy, 0 }, { ResourceType.Gems, 0 },
        };

        private readonly SaveService _saveService;
        private readonly CancellationTokenSource _saveCts = new();
        private bool _isInitialized;
        private bool _isDisposed;

        public ResourceManager(SaveService saveService)
        {
            _saveService = saveService;
        }
        
        void IStartable.Start()
        {
            //TODO refactor this
            InitializeAsync(_saveCts.Token).Forget();
        }
        
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            ThrowIfDisposed();
            if (_isInitialized)
            {
                return;
            }

            await _saveService.LoadAllAsync(ct);
            var saveData = await _saveService.GetReadonlyModuleAsync(
                data => new ResourcesModuleSaveData
                {
                    Version = data.Resources.Version,
                    Gold = data.Resources.Gold,
                    Energy = data.Resources.Energy,
                    Gems = data.Resources.Gems,
                }, ct);
            ApplySaveData(saveData);
            _isInitialized = true;
        }

        public UniTask SaveAsync(CancellationToken ct)
        {
            ThrowIfDisposed();
            var saveData = CreateSaveData();
            return _saveService.UpdateModuleAsync(data => data.Resources, resources =>
            {
                resources.Version = saveData.Version;
                resources.Gold = saveData.Gold;
                resources.Energy = saveData.Energy;
                resources.Gems = saveData.Gems;
            }, ct);
        }

        public void Add(ResourceType type, int amount)
        {
            ThrowIfDisposed();
            if (amount <= 0)
            {
                return;
            }

            _amountByType[type] += amount;
            QueueSave();
        }

        public void NotifyAmountChanged(ResourceType type)
        {
            ThrowIfDisposed();
            ResourceAmountChanged?.Invoke(type, _amountByType[type]);
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
        }

        private void ApplySaveData(ResourcesModuleSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            _amountByType[ResourceType.Gold] = Mathf.Max(0, saveData.Gold);
            _amountByType[ResourceType.Energy] = Mathf.Max(0, saveData.Energy);
            _amountByType[ResourceType.Gems] = Mathf.Max(0, saveData.Gems);
        }

        private ResourcesModuleSaveData CreateSaveData()
        {
            return new ResourcesModuleSaveData
            {
                Version = 1,
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