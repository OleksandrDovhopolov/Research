using System;
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

        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IRewardResponseApplier _rewardResponseApplier;
        private readonly IWebClient _webClient;
        private readonly SemaphoreSlim _grantMutex = new(1, 1);

        public ServerRewardGrantService(
            IPlayerIdentityProvider playerIdentityProvider,
            IWebClient webClient,
            IRewardResponseApplier rewardResponseApplier)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            _rewardResponseApplier = rewardResponseApplier ?? throw new ArgumentNullException(nameof(rewardResponseApplier));
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

                var applied = await _rewardResponseApplier.TryApplyAsync(body, ct);
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
                return RewardGrantDetailedResult.BuildSuccess(finalRewardId);
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

    }
}
