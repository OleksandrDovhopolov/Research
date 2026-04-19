using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using UnityEngine.Networking;

namespace Rewards
{
    public sealed class ServerRewardGrantService : IRewardGrantService
    {
        private const string RewardSource = "client";
        private const string GrantRewardPath = "rewards/grant";

        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IRewardGrantSnapshotHandler _snapshotHandler;

        public ServerRewardGrantService(
            IPlayerIdentityProvider playerIdentityProvider,
            IRewardGrantSnapshotHandler snapshotHandler)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _snapshotHandler = snapshotHandler ?? throw new ArgumentNullException(nameof(snapshotHandler));
        }

        public async UniTask<bool> TryGrantAsync(string rewardId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                Debug.LogWarning("[Rewards] Reward id is empty.");
                return false;
            }

            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                Debug.LogWarning("[Rewards] Player id is empty.");
                return false;
            }

            var command = new GrantRewardCommand
            {
                PlayerId = playerId,
                RewardSource = RewardSource,
                RewardId = rewardId
            };

            try
            {
                using var request = new UnityWebRequest(ApiConfig.BaseUrl + GrantRewardPath, UnityWebRequest.kHttpVerbPOST);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest().ToUniTask(cancellationToken: ct);
                if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning(
                        $"[Rewards] Grant request failed. Status={(int)request.responseCode}, Error={request.error}, RewardId={rewardId}");
                    return false;
                }

                var responseText = request.downloadHandler?.text;
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    Debug.LogWarning($"[Rewards] Grant response is empty. RewardId={rewardId}");
                    return false;
                }

                var body = JsonUtility.FromJson<GrantRewardResponseBody>(responseText);
                if (body == null)
                {
                    Debug.LogWarning($"[Rewards] Grant response is invalid JSON. RewardId={rewardId}");
                    return false;
                }

                if (!body.success)
                {
                    Debug.LogWarning(
                        $"[Rewards] Grant rejected. RewardId={rewardId}, Code={body.errorCode ?? "<none>"}, Message={body.errorMessage ?? "<none>"}");
                    return false;
                }

                if (body.snapshot == null)
                {
                    Debug.LogWarning($"[Rewards] Grant response has no snapshot. RewardId={rewardId}");
                    return false;
                }

                await _snapshotHandler.ApplyAsync(body.snapshot, ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Rewards] Grant request failed for RewardId={rewardId}. Reason={exception.Message}");
                return false;
            }
        }

        public UniTask<bool> TryGrantAsync(List<RewardGrantRequest> rewards, CancellationToken ct = default)
        {
            throw new NotSupportedException("Use TryGrantAsync(string rewardId, ...)");
        }
    }

    [Serializable]
    public sealed class GrantRewardCommand
    {
        public string PlayerId = string.Empty;
        public string RewardSource = string.Empty;
        public string RewardId = string.Empty;
    }

    [Serializable]
    public sealed class GrantRewardResponse
    {
        public bool Success;
        public string ErrorCode;
        public string ErrorMessage;
        public GrantRewardSnapshotDto Snapshot;
    }

    [Serializable]
    public sealed class GrantRewardSnapshotDto
    {
    }

    [Serializable]
    internal sealed class GrantRewardResponseBody
    {
        public bool success;
        public string errorCode;
        public string errorMessage;
        public GrantRewardSnapshotDto snapshot;
    }
}
