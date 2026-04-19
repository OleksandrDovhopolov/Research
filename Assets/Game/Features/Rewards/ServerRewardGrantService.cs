using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IReadOnlyList<IPlayerStateSnapshotHandler> _snapshotHandlers;

        public ServerRewardGrantService(
            IPlayerIdentityProvider playerIdentityProvider,
            IEnumerable<IPlayerStateSnapshotHandler> snapshotHandlers)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            if (snapshotHandlers == null)
            {
                throw new ArgumentNullException(nameof(snapshotHandlers));
            }

            _snapshotHandlers = snapshotHandlers.Where(handler => handler != null).ToList();
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
                
                return await TryApplyGrantResponseAsync(body, ct);
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

        public async UniTask<bool> TryApplyGrantResponseAsync(GrantRewardResponse grantResponse, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (grantResponse == null)
            {
                Debug.LogWarning("[Rewards] Grant response is null.");
                return false;
            }

            var rewardId = string.IsNullOrWhiteSpace(grantResponse.RewardId) ? "<unknown>" : grantResponse.RewardId;
            if (!grantResponse.Success)
            {
                Debug.LogWarning(
                    $"[Rewards] Grant rejected. RewardId={rewardId}, Code={grantResponse.ErrorCode ?? "<none>"}, Message={grantResponse.ErrorMessage ?? "<none>"}");
                return false;
            }

            if (grantResponse.PlayerState == null)
            {
                Debug.LogWarning($"[Rewards] Grant response has no playerState. RewardId={rewardId}");
                return false;
            }

            await ApplySnapshotAsync(grantResponse.PlayerState, ct);
            return true;
        }

        private async UniTask ApplySnapshotAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (snapshot == null)
            {
                return;
            }

            foreach (var snapshotHandler in _snapshotHandlers)
            {
                ct.ThrowIfCancellationRequested();
                await snapshotHandler.ApplyAsync(snapshot, ct);
            }
        }
    }
}
