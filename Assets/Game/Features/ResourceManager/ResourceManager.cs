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
    public class ResourceManager : IStartable, IDisposable
    {
        public delegate void ResourceAmountChangedHandler(ResourceType type, int newAmount);

        public event ResourceAmountChangedHandler ResourceAmountChanged;

        public const string CheatAddReason = "cheat_add";
        public const string RewardGrantReason = "reward_grant";
        public const string CheatRemoveReason = "cheat_remove";

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
            InitializeAsync(_saveCts.Token).Forget();
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            ThrowIfDisposed();
            if (_isInitialized)
            {
                return;
            }

            if (_saveService == null)
            {
                _isInitialized = true;
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

        public void NotifyAmountChanged(ResourceType type)
        {
            ThrowIfDisposed();
            ResourceAmountChanged?.Invoke(type, _amountByType[type]);
        }

        public int Get(ResourceType type)
        {
            return _amountByType[type];
        }

        public async UniTask ApplySnapshotAsync(ResourceSnapshotDto snapshot, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ct.ThrowIfCancellationRequested();
            if (snapshot == null)
            {
                return;
            }

            if (!_isInitialized)
            {
                await InitializeAsync(ct);
            }

            var normalizedSnapshot = new ResourceSnapshotDto
            {
                Gold = Mathf.Max(0, snapshot.Gold),
                Energy = Mathf.Max(0, snapshot.Energy),
                Gems = Mathf.Max(0, snapshot.Gems)
            };

            ApplyNormalizedAmounts(normalizedSnapshot.Gold, normalizedSnapshot.Energy, normalizedSnapshot.Gems);
        }

        public async UniTask ApplySnapshotAsync(IReadOnlyDictionary<string, int> resourcesById, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ct.ThrowIfCancellationRequested();
            if (resourcesById == null || resourcesById.Count == 0)
            {
                return;
            }

            if (!_isInitialized)
            {
                await InitializeAsync(ct);
            }

            var gold = _amountByType[ResourceType.Gold];
            var energy = _amountByType[ResourceType.Energy];
            var gems = _amountByType[ResourceType.Gems];

            if (TryGetResourceAmount(resourcesById, ResourceType.Gold, out var snapshotGold))
            {
                gold = snapshotGold;
            }

            if (TryGetResourceAmount(resourcesById, ResourceType.Energy, out var snapshotEnergy))
            {
                energy = snapshotEnergy;
            }

            if (TryGetResourceAmount(resourcesById, ResourceType.Gems, out var snapshotGems))
            {
                gems = snapshotGems;
            }

            var normalizedSnapshot = new ResourceSnapshotDto
            {
                Gold = Mathf.Max(0, gold),
                Energy = Mathf.Max(0, energy),
                Gems = Mathf.Max(0, gems)
            };

            ApplyNormalizedAmounts(normalizedSnapshot.Gold, normalizedSnapshot.Energy, normalizedSnapshot.Gems);
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

        private void ApplyNormalizedAmounts(int gold, int energy, int gems)
        {
            var changedGold = _amountByType[ResourceType.Gold] != gold;
            var changedEnergy = _amountByType[ResourceType.Energy] != energy;
            var changedGems = _amountByType[ResourceType.Gems] != gems;

            _amountByType[ResourceType.Gold] = gold;
            _amountByType[ResourceType.Energy] = energy;
            _amountByType[ResourceType.Gems] = gems;

            if (changedGold)
            {
                ResourceAmountChanged?.Invoke(ResourceType.Gold, gold);
            }

            if (changedEnergy)
            {
                ResourceAmountChanged?.Invoke(ResourceType.Energy, energy);
            }

            if (changedGems)
            {
                ResourceAmountChanged?.Invoke(ResourceType.Gems, gems);
            }
        }

        private static bool TryGetResourceAmount(
            IReadOnlyDictionary<string, int> resourcesById,
            ResourceType type,
            out int value)
        {
            var resourceId = type.ToString();
            if (resourcesById.TryGetValue(resourceId, out value))
            {
                return true;
            }

            foreach (var pair in resourcesById)
            {
                if (string.Equals(pair.Key, resourceId, StringComparison.OrdinalIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = 0;
            return false;
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
