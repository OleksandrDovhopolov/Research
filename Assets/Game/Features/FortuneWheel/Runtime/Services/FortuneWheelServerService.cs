using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using UnityEngine.Networking;

namespace FortuneWheel
{
    public sealed class FortuneWheelServerService : IFortuneWheelServerService
    {
        private const string DataUrl = "wheel/data";
        private const string RewardsUrl = "wheel/rewards";
        private const string SpinUrl = "wheel/spin";

        private readonly IPlayerIdentityProvider _playerIdentityProvider;

        public FortuneWheelServerService(IPlayerIdentityProvider playerIdentityProvider)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
        }
        
        public async UniTask<FortuneWheelDataServerItem> GetDataSync(CancellationToken ct = default)
        {
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            var encodedPlayerId = UnityWebRequest.EscapeURL(playerId);
            var requestUrl = $"{ApiConfig.BaseUrl}{DataUrl}?playerId={encodedPlayerId}";
            using var request = UnityWebRequest.Get(requestUrl);
            request.downloadHandler = new DownloadHandlerBuffer();

            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            ThrowIfFailed(request, "GetData");

            var responseText = request.downloadHandler?.text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("GetData response payload is empty.");
            }

            var response = JsonUtility.FromJson<WheelDataResponseBody>(responseText);
            if (response == null)
            {
                throw new InvalidOperationException("GetData response payload is invalid.");
            }

            return new FortuneWheelDataServerItem(
                Mathf.Max(0, response.availableSpins),
                Mathf.Max(0, response.nextRegenSeconds));
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
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            var requestBody = JsonUtility.ToJson(new SpinRequestBody
            {
                playerId = playerId
            });

            using var request = new UnityWebRequest(ApiConfig.BaseUrl + SpinUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            ThrowIfFailed(request, "Spin");

            var responseText = request.downloadHandler?.text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("Spin response payload is empty.");
            }

            var response = JsonUtility.FromJson<SpinResponseBody>(responseText);
            if (response == null || string.IsNullOrWhiteSpace(response.rewardId))
            {
                throw new InvalidOperationException("Spin response payload is invalid.");
            }

            return new FortuneWheelSpinResult(
                response.rewardId,
                Mathf.Max(0, response.availableSpins),
                Mathf.Max(0, response.nextRegenSeconds));
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
