using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Core.Models;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using UnityEngine.Networking;

namespace FortuneWheel
{
    public sealed class FortuneWheelServerService : IFortuneWheelServerService
    {
        private const string LogPrefix = "[FortuneWheelServerService]";
        private const string DataUrl = "wheel/data";
        private const string RewardsUrl = "wheel/rewards";
        private const string SpinUrl = "wheel/spin";

        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly SaveService _saveService;

        public FortuneWheelServerService(IPlayerIdentityProvider playerIdentityProvider, SaveService saveService)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }
        
        public async UniTask<FortuneWheelDataServerItem> GetDataSync(CancellationToken ct = default)
        {
            var cachedData = await GetCachedDataSafeAsync(ct);
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            try
            {
                var encodedPlayerId = UnityWebRequest.EscapeURL(playerId);
                var requestUrl = $"{ApiConfig.BaseUrl}{DataUrl}?playerId={encodedPlayerId}";
                Debug.Log($"{LogPrefix} GetData request. Url={requestUrl}, PlayerId={MaskPlayerId(playerId)}");
                using var request = UnityWebRequest.Get(requestUrl);
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest().ToUniTask(cancellationToken: ct);
                ThrowIfFailed(request, "GetData");

                var responseText = request.downloadHandler?.text;
                Debug.Log($"{LogPrefix} GetData response. {BuildResponseSummary(request, responseText)}");
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    throw new InvalidOperationException("GetData response payload is empty.");
                }

                var response = JsonUtility.FromJson<WheelDataResponseBody>(responseText);
                if (response == null)
                {
                    throw new InvalidOperationException("GetData response payload is invalid.");
                }

                var availableSpins = Mathf.Max(0, response.availableSpins);
                var nextRegenSeconds = Mathf.Max(0, response.nextRegenSeconds);
                Debug.Log($"{LogPrefix} GetData parsed. PlayerId={MaskPlayerId(playerId)}, AvailableSpins={availableSpins}, NextRegenSeconds={nextRegenSeconds}");
                await TryPersistCachedDataAsync(cachedData, availableSpins, ct);

                return new FortuneWheelDataServerItem(availableSpins, nextRegenSeconds);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var fallbackSpins = Mathf.Max(0, cachedData.AvailableSpins);
                var verificationUrl = BuildWheelDataUrl(playerId);
                Debug.LogWarning(
                    $"{LogPrefix} GetData failed. Falling back to cached data. PlayerId={MaskPlayerId(playerId)}, VerificationUrl={verificationUrl}, " +
                    $"CachedAvailableSpins={fallbackSpins}, CachedLastResetTimestamp={Math.Max(0, cachedData.LastResetTimestamp)}, Reason={exception}");
                return new FortuneWheelDataServerItem(fallbackSpins, 0);
            }
        }
        
        public async UniTask<IReadOnlyList<FortuneWheelRewardServerItem>> GetRewardsAsync(CancellationToken ct = default)
        {
            using var request = UnityWebRequest.Get(ApiConfig.BaseUrl + RewardsUrl);
            request.downloadHandler = new DownloadHandlerBuffer();

            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            ThrowIfFailed(request, "GetRewards");

            var responseText = request.downloadHandler?.text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return Array.Empty<FortuneWheelRewardServerItem>();
            }

            var normalizedPayload = responseText.TrimStart().StartsWith("[", StringComparison.Ordinal)
                ? "{\"rewards\":" + responseText + "}"
                : responseText;
            var response = JsonUtility.FromJson<RewardsResponseWrapper>(normalizedPayload);

            var rewardItems = response?.rewards;
            if ((rewardItems == null || rewardItems.Length == 0) && response?.items != null && response.items.Length > 0)
            {
                // Backward compatibility for previous payload shape using "items".
                rewardItems = response.items;
            }

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
            var cachedData = await GetCachedDataSafeAsync(ct);
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            var requestBody = JsonUtility.ToJson(new SpinRequestBody
            {
                playerId = playerId
            });

            var spinUrl = ApiConfig.BaseUrl + SpinUrl;
            Debug.Log($"{LogPrefix} Spin request. Url={spinUrl}, PlayerId={MaskPlayerId(playerId)}, RequestBodyLength={requestBody.Length}");

            try
            {
                using var request = new UnityWebRequest(spinUrl, UnityWebRequest.kHttpVerbPOST);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest().ToUniTask(cancellationToken: ct);
                ThrowIfFailed(request, "Spin");

                var responseText = request.downloadHandler?.text;
                Debug.Log($"{LogPrefix} Spin response. {BuildResponseSummary(request, responseText)}");
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    throw new InvalidOperationException("Spin response payload is empty.");
                }

                var response = JsonUtility.FromJson<SpinResponseBody>(responseText);
                if (response == null || string.IsNullOrWhiteSpace(response.rewardId))
                {
                    throw new InvalidOperationException("Spin response payload is invalid.");
                }

                var availableSpins = Mathf.Max(0, response.availableSpins);
                var nextRegenSeconds = Mathf.Max(0, response.nextRegenSeconds);
                Debug.Log(
                    $"{LogPrefix} Spin parsed. PlayerId={MaskPlayerId(playerId)}, RewardId={response.rewardId}, " +
                    $"AvailableSpins={availableSpins}, NextRegenSeconds={nextRegenSeconds}");
                await TryPersistCachedDataAsync(cachedData, availableSpins, ct);

                return new FortuneWheelSpinResult(
                    response.rewardId,
                    availableSpins,
                    nextRegenSeconds);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LogPrefix} Spin canceled. PlayerId={MaskPlayerId(playerId)}");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"{LogPrefix} Spin failed. PlayerId={MaskPlayerId(playerId)}, VerificationUrl={BuildWheelDataUrl(playerId)}, Reason={exception}");
                throw;
            }
        }

        private async UniTask<FortuneWheelModuleSaveData> GetCachedDataSafeAsync(CancellationToken ct)
        {
            try
            {
                return await _saveService.GetReadonlyModuleAsync(data => new FortuneWheelModuleSaveData
                {
                    AvailableSpins = data.FortuneWheel?.AvailableSpins ?? 0,
                    LastResetTimestamp = data.FortuneWheel?.LastResetTimestamp ?? 0
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

        private async UniTask TryPersistCachedDataAsync(FortuneWheelModuleSaveData cachedData, int availableSpins, CancellationToken ct)
        {
            var normalizedSpins = Mathf.Max(0, availableSpins);
            var previousSpins = Mathf.Max(0, cachedData?.AvailableSpins ?? 0);
            var previousResetTimestamp = Math.Max(0, cachedData?.LastResetTimestamp ?? 0);
            var shouldUpdateResetTimestamp = normalizedSpins > previousSpins;
            var resetTimestamp = shouldUpdateResetTimestamp
                ? DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                : previousResetTimestamp;

            try
            {
                await _saveService.UpdateModuleAsync(data => data, root =>
                {
                    root.FortuneWheel ??= new FortuneWheelModuleSaveData();
                    root.FortuneWheel.AvailableSpins = normalizedSpins;
                    root.FortuneWheel.LastResetTimestamp = Math.Max(0, resetTimestamp);
                }, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{LogPrefix} Failed to persist FortuneWheel cache: {exception.Message}");
            }
        }

        private static string BuildResponseSummary(UnityWebRequest request, string responseText)
        {
            return $"Status={(int)request.responseCode}, Result={request.result}, Error={request.error}, BodyLength={(responseText?.Length ?? 0)}";
        }

        private static string BuildWheelDataUrl(string playerId)
        {
            var encodedPlayerId = UnityWebRequest.EscapeURL(playerId ?? string.Empty);
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

        private static void ThrowIfFailed(UnityWebRequest request, string operationName)
        {
            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                var body = request.downloadHandler?.text;
                throw new InvalidOperationException(
                    $"[FortuneWheelServerService] {operationName} failed. Status={(int)request.responseCode}, Error={request.error}, Body={body}");
            }
        }

        [Serializable]
        private sealed class RewardsResponseWrapper
        {
            public RewardItemBody[] rewards;
            public RewardItemBody[] items;
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
            public int nextRegenSeconds;
        }

        [Serializable]
        private sealed class WheelDataResponseBody
        {
            public int availableSpins;
            public int nextRegenSeconds;
        }
    }
}
