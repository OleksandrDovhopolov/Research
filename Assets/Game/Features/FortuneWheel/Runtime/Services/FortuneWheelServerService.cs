using System;
using System.Collections.Generic;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Newtonsoft.Json;
using Rewards;
using UnityEngine;

namespace FortuneWheel
{
    public sealed class FortuneWheelServerService : IFortuneWheelServerService
    {
        private const string LogPrefix = "[FortuneWheelServerService]";
        private const string DataUrl = "wheel/data";
        private const string RewardsUrl = "wheel/rewards";
        private const string SpinUrl = "wheel/spin";

        private readonly IRewardSpecProvider _rewardSpecProvider;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly SaveService _saveService;
        private readonly IWebClient _webClient;

        public FortuneWheelServerService(
            IPlayerIdentityProvider playerIdentityProvider, 
            IRewardGrantService rewardGrantService,
            IRewardSpecProvider rewardSpecProvider,
            SaveService saveService,
            IWebClient webClient)
        {
            _rewardSpecProvider = rewardSpecProvider ?? throw new ArgumentNullException(nameof(rewardSpecProvider));
            _rewardGrantService = rewardGrantService ?? throw new ArgumentNullException(nameof(rewardGrantService));
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }
        
        public async UniTask<FortuneWheelDataServerItem> GetDataSync(CancellationToken ct = default)
        {
            var operationId = CreateOperationId("data");
            var cachedData = await GetCachedDataSafeAsync(ct);
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            try
            {
                var encodedPlayerId = Uri.EscapeDataString(playerId);
                var requestUrl = $"{DataUrl}?playerId={encodedPlayerId}";
                Debug.Log(
                    $"{LogPrefix} [{operationId}] GetData request. Url={requestUrl}, PlayerId={MaskPlayerId(playerId)}, " +
                    $"CachedBefore={BuildCacheSummary(cachedData)}");
                var response = await _webClient.GetAsync<WheelDataResponseBody>(requestUrl, ct);
                if (response == null)
                {
                    throw new InvalidOperationException("GetData response payload is invalid.");
                }

                var availableSpins = Mathf.Max(0, response.availableSpins);
                var updatedAt = Math.Max(0L, response.updatedAt);
                var nextUpdateAt = Math.Max(0L, response.nextUpdateAt);
                Debug.Log(
                    $"{LogPrefix} [{operationId}] GetData parsed. PlayerId={MaskPlayerId(playerId)}, " +
                    $"RawAvailableSpins={response.availableSpins}, RawUpdatedAt={response.updatedAt}, RawNextUpdateAt={response.nextUpdateAt}, " +
                    $"NormalizedAvailableSpins={availableSpins}, NormalizedUpdatedAt={updatedAt}, NormalizedNextUpdateAt={nextUpdateAt}");
                //await TryPersistCachedDataAsync(availableSpins, updatedAt, "GetData", operationId, ct);

                return new FortuneWheelDataServerItem(availableSpins, updatedAt, nextUpdateAt);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var fallbackSpins = Mathf.Max(0, cachedData.AvailableSpins);
                var fallbackUpdatedAt = Math.Max(0L, cachedData.UpdatedAt);
                const long fallbackNextUpdateAt = 0L;
                var verificationUrl = BuildWheelDataUrl(playerId);
                Debug.LogWarning(
                    $"{LogPrefix} [{operationId}] GetData failed. Falling back to cached data. PlayerId={MaskPlayerId(playerId)}, VerificationUrl={verificationUrl}, " +
                    $"CachedAvailableSpins={fallbackSpins}, CachedUpdatedAt={fallbackUpdatedAt}, " +
                    $"FallbackNextUpdateAt={fallbackNextUpdateAt} (server-only field), Reason={exception}");
                return new FortuneWheelDataServerItem(fallbackSpins, fallbackUpdatedAt, fallbackNextUpdateAt);
            }
        }
        
        public async UniTask<IReadOnlyList<FortuneWheelRewardServerItem>> GetRewardsAsync(CancellationToken ct = default)
        {
            var response = await _webClient.GetAsync<RewardsResponseWrapper>(RewardsUrl, ct);

            var rewardItems = response?.rewards;

            if (rewardItems == null || rewardItems.Length == 0)
            {
                return Array.Empty<FortuneWheelRewardServerItem>();
            }

            var result = new List<FortuneWheelRewardServerItem>(rewardItems.Length);
            for (var i = 0; i < rewardItems.Length; i++)
            {
                var rewardId = rewardItems[i]?.id;
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    rewardId = rewardItems[i]?.rewardId;
                }

                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    continue;
                }

                result.Add(new FortuneWheelRewardServerItem(rewardId));
            }

            return result;
        }

        public async UniTask<FortuneWheelSpinResult> SpinAsync(CancellationToken ct = default)
        {
            var operationId = CreateOperationId("spin");
            var cachedDataBefore = await GetCachedDataSafeAsync(ct);
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            var requestBody = JsonConvert.SerializeObject(new SpinRequestBody
            {
                playerId = playerId
            });

            Debug.Log(
                $"{LogPrefix} [{operationId}] Spin request. Url={SpinUrl}, PlayerId={MaskPlayerId(playerId)}, " +
                $"RequestBodyLength={requestBody.Length}, CachedBefore={BuildCacheSummary(cachedDataBefore)}");

            try
            {
                var response = await _webClient.PostAsync<SpinRequestBody, SpinResponseBody>(
                    SpinUrl,
                    new SpinRequestBody { playerId = playerId },
                    ct);
                if (response == null || string.IsNullOrWhiteSpace(response.rewardId))
                {
                    throw new InvalidOperationException("Spin response payload is invalid.");
                }

                var availableSpins = Mathf.Max(0, response.availableSpins);
                var updatedAt = Math.Max(0L, response.updatedAt);
                var nextUpdateAt = Math.Max(0L, response.nextUpdateAt);
                var cachedBeforeSpins = Mathf.Max(0, cachedDataBefore?.AvailableSpins ?? 0);
                var spinsDeltaVsCache = availableSpins - cachedBeforeSpins;
                Debug.Log(
                    $"{LogPrefix} [{operationId}] Spin parsed. PlayerId={MaskPlayerId(playerId)}, RewardId={response.rewardId}, " +
                    $"RawAvailableSpins={response.availableSpins}, RawUpdatedAt={response.updatedAt}, RawNextUpdateAt={response.nextUpdateAt}, " +
                    $"NormalizedAvailableSpins={availableSpins}, NormalizedUpdatedAt={updatedAt}, NormalizedNextUpdateAt={nextUpdateAt}, " +
                    $"CachedBeforeSpins={cachedBeforeSpins}, SpinsDeltaVsCache={spinsDeltaVsCache}");

                if (spinsDeltaVsCache >= 0)
                {
                    Debug.LogWarning(
                        $"{LogPrefix} [{operationId}] Spin anomaly: server did not decrease spins relative to cache. " +
                        $"CachedBeforeSpins={cachedBeforeSpins}, ServerAvailableSpins={availableSpins}, " +
                        $"RewardId={response.rewardId}, PlayerId={MaskPlayerId(playerId)}");
                }

                await TryPersistCachedDataAsync(availableSpins, updatedAt, "Spin", operationId, ct);
                var cachedDataAfter = await GetCachedDataSafeAsync(ct);
                Debug.Log($"{LogPrefix} [{operationId}] Spin cache after persist. CachedAfter={BuildCacheSummary(cachedDataAfter)}");

                /*
                 * --------------------------------------------------
                 */
                
                if (!_rewardSpecProvider.TryGet(response.rewardId, out RewardSpec rewardSpec))
                {
                    throw new InvalidOperationException($"Unknown reward id: {response.rewardId}");
                }

                var primaryResource = GetPrimaryRewardResource(rewardSpec, response.rewardId);
                var success = await _rewardGrantService.TryApplyGrantResponseAsync(response.rewardGrant, ct);
                if (!success)
                {
                    throw new InvalidOperationException($"Failed to grant reward {response.rewardId}");
                }
                
                /*
                 * --------------------------------------------------
                 */
                
                return new FortuneWheelSpinResult(
                    response.rewardId,
                    primaryResource.Icon,
                    primaryResource.Amount,
                    primaryResource.ResourceId,
                    availableSpins,
                    updatedAt,
                    nextUpdateAt);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LogPrefix} [{operationId}] Spin canceled. PlayerId={MaskPlayerId(playerId)}");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"{LogPrefix} [{operationId}] Spin failed. PlayerId={MaskPlayerId(playerId)}, VerificationUrl={BuildWheelDataUrl(playerId)}, Reason={exception}");
                throw;
            }
        }

        private static RewardSpecResource GetPrimaryRewardResource(RewardSpec rewardSpec, string rewardId)
        {
            var resources = rewardSpec?.Resources;
            if (resources == null || resources.Count == 0)
            {
                throw new InvalidOperationException($"Reward spec '{rewardId}' has no resources.");
            }

            for (var i = 0; i < resources.Count; i++)
            {
                var resource = resources[i];
                if (resource == null || string.IsNullOrWhiteSpace(resource.ResourceId) || resource.Amount <= 0)
                {
                    continue;
                }

                return resource;
            }

            throw new InvalidOperationException($"Reward spec '{rewardId}' has no primary resource.");
        }

        private async UniTask<FortuneWheelModuleSaveData> GetCachedDataSafeAsync(CancellationToken ct)
        {
            try
            {
                return await _saveService.GetReadonlyModuleAsync(data => new FortuneWheelModuleSaveData
                {
                    AvailableSpins = data.FortuneWheel?.AvailableSpins ?? 0,
                    UpdatedAt = data.FortuneWheel?.UpdatedAt ?? 0
                }, ct) ?? new FortuneWheelModuleSaveData();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{LogPrefix} Failed to load cached FortuneWheel data: {exception.Message}");
                return new FortuneWheelModuleSaveData();
            }
        }

        private async UniTask TryPersistCachedDataAsync(
            int availableSpins,
            long updatedAt,
            string source,
            string operationId,
            CancellationToken ct)
        {
            var normalizedSpins = Mathf.Max(0, availableSpins);
            var normalizedUpdatedAt = Math.Max(0L, updatedAt);
            var beforeSpins = 0;
            var beforeUpdatedAt = 0L;
            var hasBeforeSnapshot = false;

            Debug.Log(
                $"{LogPrefix} [{operationId}] Persist cache begin. Source={source}, " +
                $"IncomingAvailableSpins={availableSpins}, IncomingUpdatedAt={updatedAt}, " +
                $"NormalizedAvailableSpins={normalizedSpins}, NormalizedUpdatedAt={normalizedUpdatedAt}");

            try
            {
                await _saveService.UpdateModuleAsync(data => data, root =>
                {
                    root.FortuneWheel ??= new FortuneWheelModuleSaveData();
                    hasBeforeSnapshot = true;
                    beforeSpins = Mathf.Max(0, root.FortuneWheel.AvailableSpins);
                    beforeUpdatedAt = Math.Max(0L, root.FortuneWheel.UpdatedAt);
                    root.FortuneWheel.AvailableSpins = normalizedSpins;
                    root.FortuneWheel.UpdatedAt = normalizedUpdatedAt;
                }, ct);

                var beforeSummary = hasBeforeSnapshot
                    ? $"Spins={beforeSpins}, UpdatedAt={beforeUpdatedAt}"
                    : "<unknown>";
                Debug.Log(
                    $"{LogPrefix} [{operationId}] Persist cache completed. Source={source}, " +
                    $"Before={beforeSummary}, After=Spins={normalizedSpins}, UpdatedAt={normalizedUpdatedAt}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"{LogPrefix} [{operationId}] Failed to persist FortuneWheel cache. Source={source}, " +
                    $"Target=Spins={normalizedSpins}, UpdatedAt={normalizedUpdatedAt}, Reason={exception.Message}");
            }
        }

        private static string BuildCacheSummary(FortuneWheelModuleSaveData data)
        {
            if (data == null)
            {
                return "<null>";
            }

            return $"Spins={Mathf.Max(0, data.AvailableSpins)}, UpdatedAt={Math.Max(0L, data.UpdatedAt)}";
        }

        private static string CreateOperationId(string operationName)
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"{operationName}:{suffix}";
        }

        private static string BuildWheelDataUrl(string playerId)
        {
            var encodedPlayerId = Uri.EscapeDataString(playerId ?? string.Empty);
            return $"{ApiConfig.BaseUrl}{DataUrl}?playerId={encodedPlayerId}";
        }

        private static string MaskPlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return "<empty>";
            }

            if (playerId.Length <= 8)
            {
                return playerId;
            }

            return $"{playerId.Substring(0, 4)}...{playerId.Substring(playerId.Length - 4, 4)}";
        }

        [Serializable]
        private sealed class RewardsResponseWrapper
        {
            public RewardItemBody[] rewards;
        }

        [Serializable]
        private sealed class RewardItemBody
        {
            public string id;
            public string rewardId;
            public int weight;
        }

        [Serializable]
        private sealed class SpinRequestBody
        {
            public string playerId;
        }

        [Serializable]
        private sealed class SpinResponseBody
        {
            public string rewardId;
            public int availableSpins;
            public long updatedAt;
            public long nextUpdateAt;
            public GrantRewardResponse rewardGrant;
        }

        [Serializable]
        private sealed class WheelDataResponseBody
        {
            public int availableSpins;
            public long updatedAt;
            public long nextUpdateAt;
        }
    }
}
