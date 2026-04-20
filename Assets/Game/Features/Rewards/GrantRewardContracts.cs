using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rewards
{
    [Serializable]
    public sealed class GrantRewardCommand
    {
        [JsonProperty("playerId")]
        public string PlayerId { get; set; } = string.Empty;

        [JsonProperty("rewardSource")]
        public string RewardSource { get; set; } = string.Empty;

        [JsonProperty("rewardId")]
        public string RewardId { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class GrantRewardResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("rewardId")]
        public string RewardId { get; set; } = string.Empty;

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty("playerState")]
        public PlayerStateSnapshotDto PlayerState { get; set; }
    }

    [Serializable]
    public sealed class PlayerStateSnapshotDto
    {
        [JsonProperty("resources")]
        public Dictionary<string, int> Resources { get; set; } = new(StringComparer.Ordinal);

        [JsonProperty("inventoryItems")]
        public List<InventoryItemDto> InventoryItems { get; set; } = new();
    }

    [Serializable]
    public sealed class InventoryItemDto
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; } = string.Empty;

        [JsonProperty("amount")]
        public int Amount { get; set; }
    }
}
