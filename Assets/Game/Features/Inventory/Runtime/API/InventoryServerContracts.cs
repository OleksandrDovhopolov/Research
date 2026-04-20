using System;
using System.Collections.Generic;

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
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public InventorySnapshotDto Inventory { get; set; }
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
        public Dictionary<string, int> Resources { get; set; } = new(StringComparer.Ordinal);
        public List<PlayerStateInventoryItemDto> InventoryItems { get; set; } = new();
    }

    [Serializable]
    public sealed class PlayerStateInventoryItemDto
    {
        public string ItemId { get; set; } = string.Empty;
        public int Amount { get; set; }
    }
}
