using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Inventory.API
{
    [Serializable]
    public sealed class RemoveInventoryItemCommand
    {
        public string PlayerId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class RemoveInventoryBatchCommand
    {
        public string PlayerId { get; set; } = string.Empty;
        public List<RemoveInventoryBatchItem> Items { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class RemoveInventoryBatchItem
    {
        public string ItemId { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    [Serializable]
    public sealed class InventoryOperationResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }
        [JsonProperty("playerState")]
        public PlayerStateSnapshotResponseDto PlayerState { get; set; }
    }

    [Serializable]
    public sealed class InventorySnapshotDto
    {
        public List<InventorySnapshotItemDto> Items { get; set; } = new();
    }

    [Serializable]
    public sealed class InventorySnapshotItemDto
    {
        public string ItemId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string CategoryId { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class PlayerStateSnapshotResponseDto
    {
        [JsonProperty("resources")]
        public Dictionary<string, int> Resources { get; set; } = new(StringComparer.Ordinal);
        [JsonProperty("inventoryItems")]
        public List<PlayerStateInventoryItemDto> InventoryItems { get; set; } = new();
    }

    [Serializable]
    public sealed class PlayerStateInventoryItemDto
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; } = string.Empty;
        [JsonProperty("amount")]
        public int Amount { get; set; }
    }
}
