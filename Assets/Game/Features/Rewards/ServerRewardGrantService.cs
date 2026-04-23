using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace Rewards
{
    public sealed class ServerRewardGrantService : IRewardGrantService
    {
        private const string RewardSource = "client";
        private const string GrantRewardPath = "rewards/grant";
        private const string GemsResourceId = "Gems";

        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IReadOnlyList<IPlayerStateSnapshotHandler> _snapshotHandlers;
        private readonly IWebClient _webClient;
        private readonly SemaphoreSlim _grantMutex = new(1, 1);

        public ServerRewardGrantService(
            IPlayerIdentityProvider playerIdentityProvider,
            IWebClient webClient,
            IEnumerable<IPlayerStateSnapshotHandler> snapshotHandlers)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            if (snapshotHandlers == null)
            {
                throw new ArgumentNullException(nameof(snapshotHandlers));
            }

            _snapshotHandlers = snapshotHandlers.Where(handler => handler != null).ToList();
        }

        public async UniTask<bool> TryGrantAsync(string rewardId, CancellationToken ct = default)
        {
            var detailedResult = await TryGrantDetailedAsync(rewardId, ct);
            return detailedResult.Success;
        }

        public async UniTask<RewardGrantDetailedResult> TryGrantDetailedAsync(string rewardId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                Debug.LogWarning("[Rewards] Reward id is empty.");
                return RewardGrantDetailedResult.BuildFailure(
                    rewardId,
                    RewardGrantFailureType.InvalidResponse,
                    "EMPTY_REWARD_ID",
                    "Reward id is empty.");
            }

            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                Debug.LogWarning("[Rewards] Player id is empty.");
                return RewardGrantDetailedResult.BuildFailure(
                    rewardId,
                    RewardGrantFailureType.InvalidResponse,
                    "EMPTY_PLAYER_ID",
                    "Player id is empty.");
            }

            var command = new GrantRewardCommand
            {
                PlayerId = playerId,
                RewardSource = RewardSource,
                RewardId = rewardId
            };
            var lockTaken = false;

            try
            {
                await _grantMutex.WaitAsync(ct);
                lockTaken = true;

                var body = await _webClient.PostAsync<GrantRewardCommand, GrantRewardResponse>(GrantRewardPath, command, ct);
                if (body == null)
                {
                    Debug.LogWarning($"[Rewards] Grant response is empty. RewardId={rewardId}");
                    return RewardGrantDetailedResult.BuildFailure(
                        rewardId,
                        RewardGrantFailureType.InvalidResponse,
                        "EMPTY_RESPONSE",
                        "Grant response is empty.");
                }

                var applied = await TryApplyGrantResponseAsync(body, ct);
                if (!applied)
                {
                    var normalizedRewardId = string.IsNullOrWhiteSpace(body.RewardId) ? rewardId : body.RewardId;
                    return RewardGrantDetailedResult.BuildFailure(
                        normalizedRewardId,
                        body.Success ? RewardGrantFailureType.InvalidResponse : RewardGrantFailureType.Rejected,
                        body.ErrorCode,
                        body.ErrorMessage ?? "Grant response cannot be applied.");
                }

                var finalRewardId = string.IsNullOrWhiteSpace(body.RewardId) ? rewardId : body.RewardId;
                return RewardGrantDetailedResult.BuildSuccess(finalRewardId, TryGetCrystalsBalance(body.PlayerState));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (WebClientNetworkException exception)
            {
                Debug.LogWarning($"[Rewards] Grant request network failure for RewardId={rewardId}. Reason={exception.Message}");
                return RewardGrantDetailedResult.BuildFailure(
                    rewardId,
                    RewardGrantFailureType.Network,
                    "NETWORK_ERROR",
                    exception.Message);
            }
            catch (WebClientHttpException exception)
            {
                Debug.LogWarning($"[Rewards] Grant request HTTP failure for RewardId={rewardId}. Reason={exception.Message}");
                return RewardGrantDetailedResult.BuildFailure(
                    rewardId,
                    RewardGrantFailureType.Http,
                    exception.StatusCode.ToString(),
                    exception.Message);
            }
            catch (WebClientException exception)
            {
                Debug.LogWarning($"[Rewards] Grant request failed for RewardId={rewardId}. Reason={exception.Message}");
                return RewardGrantDetailedResult.BuildFailure(
                    rewardId,
                    RewardGrantFailureType.Unknown,
                    "WEB_CLIENT_ERROR",
                    exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Rewards] Grant request failed for RewardId={rewardId}. Reason={exception.Message}");
                return RewardGrantDetailedResult.BuildFailure(
                    rewardId,
                    RewardGrantFailureType.Unknown,
                    "UNEXPECTED_ERROR",
                    exception.Message);
            }
            finally
            {
                if (lockTaken)
                {
                    _grantMutex.Release();
                }
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

        // TODO what is it for ? reward cant be crystals all the time ?  take reward not from snapshot but from ResourceManager.GetAmount(Gems)
        private static int? TryGetCrystalsBalance(PlayerStateSnapshotDto snapshot)
        {
            if (snapshot?.Resources == null || snapshot.Resources.Count == 0)
            {
                return null;
            }

            if (snapshot.Resources.TryGetValue(GemsResourceId, out var gems))
            {
                return gems;
            }

            foreach (var pair in snapshot.Resources)
            {
                if (string.Equals(pair.Key, GemsResourceId, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return null;
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
