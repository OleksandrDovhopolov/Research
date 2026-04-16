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
        private const string RewardsUrl = "wheel/rewards";
        private const string SpinUrl = "wheel/spin";

        private readonly IPlayerIdentityProvider _playerIdentityProvider;

        public FortuneWheelServerService(IPlayerIdentityProvider playerIdentityProvider)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
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

            var wrappedJson = "{\"items\":" + responseText + "}";
            var response = JsonUtility.FromJson<RewardsResponseWrapper>(wrappedJson);
            if (response?.items == null || response.items.Length == 0)
            {
                return Array.Empty<FortuneWheelRewardServerItem>();
            }

            var result = new List<FortuneWheelRewardServerItem>(response.items.Length);
            for (var i = 0; i < response.items.Length; i++)
            {
                var rewardId = response.items[i]?.rewardId;
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
            public RewardItemBody[] items;
        }

        [Serializable]
        private sealed class RewardItemBody
        {
            public string rewardId;
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
    }
}
