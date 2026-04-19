using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Newtonsoft.Json;
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
                var payload = JsonConvert.SerializeObject(command);

                using var request = new UnityWebRequest(ApiConfig.BaseUrl + GrantRewardPath, UnityWebRequest.kHttpVerbPOST);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
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

                var body = JsonConvert.DeserializeObject<GrantRewardResponse>(responseText);
                if (body == null)
                {
                    Debug.LogWarning($"[Rewards] Grant response is invalid JSON. RewardId={rewardId}");
                    return false;
                }

                if (!body.Success)
                {
                    Debug.LogWarning(
                        $"[Rewards] Grant rejected. RewardId={rewardId}, Code={body.ErrorCode ?? "<none>"}, Message={body.ErrorMessage ?? "<none>"}");
                    return false;
                }

                if (body.PlayerState == null)
                {
                    Debug.LogWarning($"[Rewards] Grant response has no playerState. RewardId={rewardId}");
                    return false;
                }

                await _snapshotHandler.ApplyAsync(body.PlayerState, ct);
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
}
