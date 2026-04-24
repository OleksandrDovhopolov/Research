using System;
using Newtonsoft.Json;

namespace Rewards
{
    public enum RewardIntentStatus
    {
        Unknown = 0,
        Pending = 1,
        Fulfilled = 2,
        Rejected = 3,
        Failed = 4,
        Expired = 5
    }

    public sealed class CreateRewardIntentResult
    {
        public bool IsSuccess;
        public string RewardIntentId;
        public string ErrorCode;
        public string ErrorMessage;
    }

    public sealed class GetRewardIntentStatusResult
    {
        public RewardIntentStatus Status;
        public string ErrorCode;
        public string ErrorMessage;
    }

    [Serializable]
    internal sealed class CreateRewardIntentRequest
    {
        [JsonProperty("playerId")]
        public string PlayerId { get; set; } = string.Empty;

        [JsonProperty("rewardId")]
        public string RewardId { get; set; } = string.Empty;
    }

    [Serializable]
    internal sealed class CreateRewardIntentResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("rewardIntentId")]
        public string RewardIntentId { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }

    [Serializable]
    internal sealed class RewardIntentStatusResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
