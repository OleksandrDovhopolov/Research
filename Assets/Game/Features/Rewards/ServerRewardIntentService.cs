using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace Rewards
{
    public sealed class ServerRewardIntentService : IRewardIntentService
    {
        private const string CreateIntentPath = "rewards/intent/create";
        private const string GetIntentStatusPath = "rewards/intent/status";

        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IWebClient _webClient;

        public ServerRewardIntentService(
            IPlayerIdentityProvider playerIdentityProvider,
            IWebClient webClient)
        {
            _playerIdentityProvider = playerIdentityProvider ?? throw new ArgumentNullException(nameof(playerIdentityProvider));
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }

        public async UniTask<CreateRewardIntentResult> CreateAsync(string rewardId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return new CreateRewardIntentResult
                {
                    IsSuccess = false,
                    ErrorCode = "EMPTY_REWARD_ID",
                    ErrorMessage = "Reward id is empty."
                };
            }

            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return new CreateRewardIntentResult
                {
                    IsSuccess = false,
                    ErrorCode = "EMPTY_PLAYER_ID",
                    ErrorMessage = "Player id is empty."
                };
            }

            try
            {
                var response = await _webClient.PostAsync<CreateRewardIntentRequest, CreateRewardIntentResponse>(
                    CreateIntentPath,
                    new CreateRewardIntentRequest
                    {
                        PlayerId = playerId,
                        RewardId = rewardId
                    },
                    ct);

                if (response == null)
                {
                    return new CreateRewardIntentResult
                    {
                        IsSuccess = false,
                        ErrorCode = "EMPTY_RESPONSE",
                        ErrorMessage = "Create intent response is empty."
                    };
                }

                return new CreateRewardIntentResult
                {
                    IsSuccess = response.Success && !string.IsNullOrWhiteSpace(response.RewardIntentId),
                    RewardIntentId = response.RewardIntentId,
                    ErrorCode = response.ErrorCode,
                    ErrorMessage = response.ErrorMessage
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (WebClientNetworkException exception)
            {
                Debug.LogWarning($"[AdsRewardFlow] Intent create network error. Reason={exception.Message}");
                return new CreateRewardIntentResult
                {
                    IsSuccess = false,
                    ErrorCode = "NETWORK_ERROR",
                    ErrorMessage = exception.Message
                };
            }
            catch (WebClientException exception)
            {
                Debug.LogWarning($"[AdsRewardFlow] Intent create failed. Reason={exception.Message}");
                return new CreateRewardIntentResult
                {
                    IsSuccess = false,
                    ErrorCode = "WEB_CLIENT_ERROR",
                    ErrorMessage = exception.Message
                };
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AdsRewardFlow] Intent create unexpected error. Reason={exception.Message}");
                return new CreateRewardIntentResult
                {
                    IsSuccess = false,
                    ErrorCode = "UNEXPECTED_ERROR",
                    ErrorMessage = exception.Message
                };
            }
        }

        public async UniTask<GetRewardIntentStatusResult> GetStatusAsync(string rewardIntentId, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(rewardIntentId))
            {
                return new GetRewardIntentStatusResult
                {
                    Status = RewardIntentStatus.Unknown,
                    ErrorCode = "EMPTY_REWARD_INTENT_ID",
                    ErrorMessage = "Reward intent id is empty."
                };
            }

            var requestUrl = $"{GetIntentStatusPath}?rewardIntentId={Uri.EscapeDataString(rewardIntentId)}";
            var response = await _webClient.GetAsync<RewardIntentStatusResponse>(requestUrl, ct);
            if (response == null)
            {
                return new GetRewardIntentStatusResult
                {
                    Status = RewardIntentStatus.Unknown,
                    ErrorCode = "EMPTY_RESPONSE",
                    ErrorMessage = "Intent status response is empty."
                };
            }

            return new GetRewardIntentStatusResult
            {
                Status = ParseStatus(response.Status),
                NewCrystalsBalance = response.NewCrystalsBalance,
                ErrorCode = response.ErrorCode,
                ErrorMessage = response.ErrorMessage
            };
        }

        private static RewardIntentStatus ParseStatus(string rawStatus)
        {
            if (string.IsNullOrWhiteSpace(rawStatus))
            {
                return RewardIntentStatus.Unknown;
            }

            return rawStatus.Trim().ToLowerInvariant() switch
            {
                "pending" => RewardIntentStatus.Pending,
                "fulfilled" => RewardIntentStatus.Fulfilled,
                "rejected" => RewardIntentStatus.Rejected,
                "failed" => RewardIntentStatus.Failed,
                "expired" => RewardIntentStatus.Expired,
                _ => RewardIntentStatus.Unknown
            };
        }
    }
}
