using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Newtonsoft.Json;
using UnityEngine;

namespace BattlePass
{
    public sealed class StubBattlePassPurchaseVerify : IBattlePassPurchaseVerify
    {
        private readonly IWebClient _webClient;
        private readonly IBattlePassPlayerContext _playerContext;

        public StubBattlePassPurchaseVerify(IWebClient webClient, IBattlePassPlayerContext playerContext)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }
        
        public async UniTask<BattlePassPurchaseVerificationResult> VerifyGooglePurchaseAsync(string seasonId, string productId, string purchaseToken,
            CancellationToken ct = default)
        {
            var playerId = _playerContext.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            if (string.IsNullOrWhiteSpace(seasonId))
            {
                throw new InvalidOperationException("Season id is empty.");
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new InvalidOperationException("Product id is empty.");
            }

            if (string.IsNullOrWhiteSpace(purchaseToken))
            {
                throw new InvalidOperationException("Purchase token is empty.");
            }

            var request = new BattlePassPurchaseVerificationRequest
            {
                PlayerId = playerId,
                ProductId = productId,
                PurchaseToken = purchaseToken,
                SeasonId = seasonId
            };
            var response = await _webClient.PostAsync<BattlePassPurchaseVerificationRequest, BattlePassPurchaseVerificationResponse>(
                BattlePassConfig.Api.DevGrantBattlePassPath,
                request,
                ct);
            
            return MapPurchaseVerificationResult(response);
        }
        
        private static BattlePassPurchaseVerificationResult MapPurchaseVerificationResult(BattlePassPurchaseVerificationResponse response)
        {
            if (response == null)
            {
                return new BattlePassPurchaseVerificationResult(
                    false,
                    string.Empty,
                    null,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "empty_response",
                    "Battle pass purchase verification response is empty.");
            }

            var entitlementId = GetFirstNonEmpty(
                response.Entitlement?.EntitlementId,
                response.Entitlement?.Key);
            var entitlementType = GetFirstNonEmpty(
                response.Entitlement?.EntitlementType,
                response.Entitlement?.Type);
            var entitlementKey = GetFirstNonEmpty(
                response.Entitlement?.Key,
                response.Entitlement?.EntitlementId);

            return new BattlePassPurchaseVerificationResult(
                response.Success,
                response.PurchaseStatus,
                MapUserState(response.BattlePass),
                entitlementId,
                entitlementType,
                entitlementKey,
                response.Entitlement?.Status,
                response.GoogleFinalizeStatus,
                response.ErrorCode,
                response.ErrorMessage);
        }
        
        private static string GetFirstNonEmpty(string primary, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return primary;
            }

            return fallback ?? string.Empty;
        }
        
        private static BattlePassUserState MapUserState(BattlePassUserStateResponse response)
        {
            if (response == null)
            {
                return null;
            }

            var claimedRewards = MapClaimedRewards(response.ClaimedRewards);
            var claimableRewards = MapClaimableRewards(response.ClaimableRewards);

            return new BattlePassUserState(
                response.SeasonId,
                response.Level,
                response.Xp,
                MapPassType(response.PassType),
                claimedRewards,
                claimableRewards);
        }
        
        private static IReadOnlyList<BattlePassClaimedRewardCell> MapClaimedRewards(BattlePassClaimedRewardCellResponse[] responses)
        {
            if (responses == null || responses.Length == 0)
            {
                return Array.Empty<BattlePassClaimedRewardCell>();
            }

            var result = new List<BattlePassClaimedRewardCell>(responses.Length);
            for (var i = 0; i < responses.Length; i++)
            {
                var response = responses[i];
                if (response == null)
                {
                    continue;
                }

                if (response.Level < 0)
                {
                    Debug.LogError("[BattlePassServerService] Claimed reward cell has negative level and was skipped.");
                    continue;
                }

                if (!TryParseRewardTrack(response.RewardTrack, out var rewardTrack))
                {
                    Debug.LogError($"[BattlePassServerService] Claimed reward cell has unsupported rewardTrack '{response.RewardTrack}' and was skipped.");
                    continue;
                }

                result.Add(new BattlePassClaimedRewardCell(
                    response.Level,
                    rewardTrack,
                    ParseUtcOrFallback(response.ClaimedAtUtc, DateTimeOffset.MinValue)));
            }

            return result;
        }
        
        private static IReadOnlyList<BattlePassClaimableRewardCell> MapClaimableRewards(BattlePassClaimableRewardCellResponse[] responses)
        {
            if (responses == null || responses.Length == 0)
            {
                return Array.Empty<BattlePassClaimableRewardCell>();
            }

            var result = new List<BattlePassClaimableRewardCell>(responses.Length);
            for (var i = 0; i < responses.Length; i++)
            {
                var response = responses[i];
                if (response == null)
                {
                    continue;
                }

                if (response.Level < 0)
                {
                    Debug.LogError("[BattlePassServerService] Claimable reward cell has negative level and was skipped.");
                    continue;
                }

                if (!TryParseRewardTrack(response.RewardTrack, out var rewardTrack))
                {
                    Debug.LogError($"[BattlePassServerService] Claimable reward cell has unsupported rewardTrack '{response.RewardTrack}' and was skipped.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(response.RewardId))
                {
                    Debug.LogError("[BattlePassServerService] Claimable reward cell has empty rewardId and was skipped.");
                    continue;
                }

                result.Add(new BattlePassClaimableRewardCell(
                    response.Level,
                    rewardTrack,
                    response.RewardId));
            }

            return result;
        }

        private static bool TryParseRewardTrack(string rewardTrack, out BattlePassRewardTrack track)
        {
            switch (rewardTrack?.Trim().ToLowerInvariant())
            {
                case "default":
                    track = BattlePassRewardTrack.Default;
                    return true;
                case "premium":
                    track = BattlePassRewardTrack.Premium;
                    return true;
                default:
                    track = default;
                    return false;
            }
        }

        private static DateTimeOffset ParseUtcOrFallback(string rawValue, DateTimeOffset fallback)
        {
            return DateTimeOffset.TryParse(
                rawValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed)
                ? parsed
                : fallback;
        }

        private static BattlePassPassType MapPassType(string passType)
        {
            if (string.IsNullOrWhiteSpace(passType))
            {
                return BattlePassPassType.Unknown;
            }

            return passType.Trim().ToLowerInvariant() switch
            {
                "none" => BattlePassPassType.None,
                "gold" => BattlePassPassType.Premium,
                "premium" => BattlePassPassType.Premium,
                "platinum" => BattlePassPassType.Platinum,
                _ => BattlePassPassType.Unknown
            };
        }
        
        [Serializable]
        private sealed class BattlePassPurchaseVerificationRequest
        {
            [JsonProperty("playerId")]
            public string PlayerId { get; set; }

            [JsonProperty("productId")]
            public string ProductId { get; set; }

            [JsonProperty("purchaseToken")]
            public string PurchaseToken { get; set; }

            [JsonProperty("seasonId")]
            public string SeasonId { get; set; }
        }
        
        [Serializable]
        private sealed class BattlePassPurchaseVerificationResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("purchaseStatus")]
            public string PurchaseStatus { get; set; }

            [JsonProperty("entitlement")]
            public BattlePassEntitlementResponse Entitlement { get; set; }

            [JsonProperty("battlePass")]
            public BattlePassUserStateResponse BattlePass { get; set; }

            [JsonProperty("googleFinalizeStatus")]
            public string GoogleFinalizeStatus { get; set; }

            [JsonProperty("errorCode")]
            public string ErrorCode { get; set; }

            [JsonProperty("errorMessage")]
            public string ErrorMessage { get; set; }
        }
        [Serializable]
        private sealed class BattlePassEntitlementResponse
        {
            [JsonProperty("entitlementId")]
            public string EntitlementId { get; set; }

            [JsonProperty("entitlementType")]
            public string EntitlementType { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }
        }
        
        [Serializable]
        private sealed class BattlePassUserStateResponse
        {
            [JsonProperty("seasonId")]
            public string SeasonId { get; set; }

            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("xp")]
            public int Xp { get; set; }

            [JsonProperty("passType")]
            public string PassType { get; set; }

            [JsonProperty("claimedRewards")]
            public BattlePassClaimedRewardCellResponse[] ClaimedRewards { get; set; }

            [JsonProperty("claimableRewards")]
            public BattlePassClaimableRewardCellResponse[] ClaimableRewards { get; set; }
        }
        
        [Serializable]
        private sealed class BattlePassClaimedRewardCellResponse
        {
            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("rewardTrack")]
            public string RewardTrack { get; set; }

            [JsonProperty("claimedAtUtc")]
            public string ClaimedAtUtc { get; set; }
        }

        [Serializable]
        private sealed class BattlePassClaimableRewardCellResponse
        {
            [JsonProperty("level")]
            public int Level { get; set; }

            [JsonProperty("rewardTrack")]
            public string RewardTrack { get; set; }

            [JsonProperty("rewardId")]
            public string RewardId { get; set; }
        }
    }
}
