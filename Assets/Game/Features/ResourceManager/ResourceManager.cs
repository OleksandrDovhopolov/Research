using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using UnityEngine.Networking;
using VContainer.Unity;

namespace CoreResources
{
    public class ResourceManager : IStartable
    {
        public delegate void ResourceAmountChangedHandler(ResourceType type, int newAmount);

        public event ResourceAmountChangedHandler ResourceAmountChanged;

        public const string RewardGrantReason = "reward_grant";
        public const string CheatAddReason = "cheat_add";
        public const string CheatRemoveReason = "cheat_remove";

        private readonly Dictionary<ResourceType, int> _amountByType = new()
        {
            { ResourceType.Gold, 0 }, { ResourceType.Energy, 0 }, { ResourceType.Gems, 0 },
        };

        private readonly SaveService _saveService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IResourceAdjustApi _resourceAdjustApi;
        private readonly SemaphoreSlim _adjustSemaphore = new(1, 1);
        private readonly CancellationTokenSource _saveCts = new();
        private bool _isInitialized;
        private bool _isDisposed;

        public ResourceManager(SaveService saveService, IPlayerIdentityProvider playerIdentityProvider)
            : this(saveService, playerIdentityProvider, null)
        {
        }

        public ResourceManager(SaveService saveService, IPlayerIdentityProvider playerIdentityProvider, IResourceAdjustApi resourceAdjustApi)
        {
            _saveService = saveService;
            _playerIdentityProvider = playerIdentityProvider;
            _resourceAdjustApi = resourceAdjustApi ?? new UnityWebRequestResourceAdjustApi();
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

        public async UniTask Add(
            ResourceType type,
            int amount,
            string reason = RewardGrantReason,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (amount <= 0)
            {
                return;
            }

            await AdjustInternalAsync(type, amount, reason, ct);
        }

        public void NotifyAmountChanged(ResourceType type)
        {
            ThrowIfDisposed();
            ResourceAmountChanged?.Invoke(type, _amountByType[type]);
        }

        public async UniTask<bool> Remove(
            ResourceType type,
            int amount,
            string reason = CheatRemoveReason,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (amount <= 0)
            {
                return false;
            }

            await AdjustInternalAsync(type, -amount, reason, ct);
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
            _adjustSemaphore.Dispose();
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

        private async UniTask AdjustInternalAsync(ResourceType type, int delta, string reason, CancellationToken ct)
        {
            ThrowIfDisposed();
            if (!_isInitialized)
            {
                await InitializeAsync(ct);
            }

            if (_playerIdentityProvider == null)
            {
                throw new InvalidOperationException("Player identity provider is missing.");
            }

            ct.ThrowIfCancellationRequested();
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            await _adjustSemaphore.WaitAsync(ct);
            try
            {
                var request = new AdjustResourceCommand
                {
                    PlayerId = playerId,
                    ResourceId = type.ToString(),
                    Delta = delta,
                    Reason = string.IsNullOrWhiteSpace(reason) ? RewardGrantReason : reason
                };

                var response = await _resourceAdjustApi.AdjustAsync(request, ct);
                if (response == null)
                {
                    throw new InvalidOperationException("Resource adjust response is null.");
                }

                if (!response.Success)
                {
                    throw new InvalidOperationException(
                        $"Resource adjust rejected. Code={response.ErrorCode ?? "<none>"}, Message={response.ErrorMessage ?? "<none>"}");
                }

                if (response.Resources == null)
                {
                    throw new InvalidOperationException("Resource adjust response does not contain resources snapshot.");
                }

                ApplySnapshot(response.Resources);
                //await TryPersistSnapshotAsync(response.Resources, ct);
            }
            finally
            {
                _adjustSemaphore.Release();
            }
        }

        private void ApplySnapshot(ResourceSnapshotDto snapshot)
        {
            var normalizedGold = Mathf.Max(0, snapshot.Gold);
            var normalizedEnergy = Mathf.Max(0, snapshot.Energy);
            var normalizedGems = Mathf.Max(0, snapshot.Gems);

            var changedGold = _amountByType[ResourceType.Gold] != normalizedGold;
            var changedEnergy = _amountByType[ResourceType.Energy] != normalizedEnergy;
            var changedGems = _amountByType[ResourceType.Gems] != normalizedGems;

            _amountByType[ResourceType.Gold] = normalizedGold;
            _amountByType[ResourceType.Energy] = normalizedEnergy;
            _amountByType[ResourceType.Gems] = normalizedGems;

            if (changedGold)
            {
                ResourceAmountChanged?.Invoke(ResourceType.Gold, normalizedGold);
            }

            if (changedEnergy)
            {
                ResourceAmountChanged?.Invoke(ResourceType.Energy, normalizedEnergy);
            }

            if (changedGems)
            {
                ResourceAmountChanged?.Invoke(ResourceType.Gems, normalizedGems);
            }
        }

        private async UniTask TryPersistSnapshotAsync(ResourceSnapshotDto snapshot, CancellationToken ct)
        {
            if (_saveService == null || !_isInitialized)
            {
                return;
            }

            try
            {
                await _saveService.UpdateModuleAsync(data => data.Resources, resources =>
                {
                    resources.Version = Math.Max(1, resources.Version);
                    resources.Gold = Mathf.Max(0, snapshot.Gold);
                    resources.Energy = Mathf.Max(0, snapshot.Energy);
                    resources.Gems = Mathf.Max(0, snapshot.Gems);
                }, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ResourceManager] Failed to persist resource snapshot: {exception}");
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

    public interface IResourceAdjustApi
    {
        UniTask<AdjustResourceResponse> AdjustAsync(AdjustResourceCommand command, CancellationToken ct);
    }

    [Serializable]
    public sealed class AdjustResourceCommand
    {
        public string PlayerId = string.Empty;
        public string ResourceId = string.Empty;
        public int Delta;
        public string Reason = string.Empty;
    }

    [Serializable]
    public sealed class AdjustResourceResponse
    {
        public bool Success;
        public string ErrorCode;
        public string ErrorMessage;
        public ResourceSnapshotDto Resources;
    }

    [Serializable]
    public sealed class ResourceSnapshotDto
    {
        public int Gold;
        public int Energy;
        public int Gems;
    }

    public sealed class UnityWebRequestResourceAdjustApi : IResourceAdjustApi
    {
        private const string AdjustResourcePath = "resources/adjust";

        public async UniTask<AdjustResourceResponse> AdjustAsync(AdjustResourceCommand command, CancellationToken ct)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var payload = JsonUtility.ToJson(new AdjustResourceRequestBody
            {
                playerId = command.PlayerId,
                resourceId = command.ResourceId,
                delta = command.Delta,
                reason = command.Reason
            });

            using var request = new UnityWebRequest(ApiConfig.BaseUrl + AdjustResourcePath, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest().ToUniTask(cancellationToken: ct);

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                var body = request.downloadHandler?.text;
                throw new InvalidOperationException(
                    $"Resource adjust request failed. Status={(int)request.responseCode}, Error={request.error}, Body={body}");
            }

            var responseText = request.downloadHandler?.text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("Resource adjust response is empty.");
            }

            var parsed = JsonUtility.FromJson<AdjustResourceResponseBody>(responseText);
            if (parsed == null)
            {
                throw new InvalidOperationException("Resource adjust response payload is invalid.");
            }

            return new AdjustResourceResponse
            {
                Success = parsed.success,
                ErrorCode = parsed.errorCode,
                ErrorMessage = parsed.errorMessage,
                Resources = parsed.resources == null
                    ? null
                    : new ResourceSnapshotDto
                    {
                        Gold = parsed.resources.gold,
                        Energy = parsed.resources.energy,
                        Gems = parsed.resources.gems
                    }
            };
        }

        [Serializable]
        private sealed class AdjustResourceRequestBody
        {
            public string playerId;
            public string resourceId;
            public int delta;
            public string reason;
        }

        [Serializable]
        private sealed class AdjustResourceResponseBody
        {
            public bool success;
            public string errorCode;
            public string errorMessage;
            public ResourceSnapshotResponseBody resources;
        }

        [Serializable]
        private sealed class ResourceSnapshotResponseBody
        {
            public int gold;
            public int energy;
            public int gems;
        }
    }
}
