using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        [JsonProperty("inventory")]
        public InventorySnapshotDto Inventory { get; set; }
        [JsonProperty("playerState")]
        public PlayerStateSnapshotResponseDto PlayerState { get; set; }
    }

    [Serializable]
    [JsonConverter(typeof(InventorySnapshotDtoConverter))]
    public sealed class InventorySnapshotDto
    {
        [JsonProperty("items")]
        public List<InventorySnapshotItemDto> Items { get; set; } = new();
    }

    [Serializable]
    public sealed class InventorySnapshotItemDto
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; } = string.Empty;
        [JsonProperty("amount")]
        public int Amount { get; set; }
        [JsonProperty("categoryId")]
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

    internal sealed class InventorySnapshotDtoConverter : JsonConverter<InventorySnapshotDto>
    {
        public override InventorySnapshotDto ReadJson(
            JsonReader reader,
            Type objectType,
            InventorySnapshotDto existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return new InventorySnapshotDto();
            }

            var root = JObject.Load(reader);
            var dto = existingValue ?? new InventorySnapshotDto();
            dto.Items = ParseItems(root["items"], serializer);
            return dto;
        }

        public override void WriteJson(JsonWriter writer, InventorySnapshotDto value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("items");
            serializer.Serialize(writer, value?.Items ?? new List<InventorySnapshotItemDto>());
            writer.WriteEndObject();
        }

        private static List<InventorySnapshotItemDto> ParseItems(JToken itemsToken, JsonSerializer serializer)
        {
            if (itemsToken == null || itemsToken.Type == JTokenType.Null)
            {
                return new List<InventorySnapshotItemDto>();
            }

            if (itemsToken.Type == JTokenType.Array)
            {
                return itemsToken.ToObject<List<InventorySnapshotItemDto>>(serializer) ?? new List<InventorySnapshotItemDto>();
            }

            if (itemsToken.Type != JTokenType.Object)
            {
                return new List<InventorySnapshotItemDto>();
            }

            var mapped = new List<InventorySnapshotItemDto>();
            foreach (var property in ((JObject)itemsToken).Properties())
            {
                if (string.IsNullOrWhiteSpace(property.Name))
                {
                    continue;
                }

                if (!TryReadAmount(property.Value, out var amount))
                {
                    continue;
                }

                mapped.Add(new InventorySnapshotItemDto
                {
                    ItemId = property.Name,
                    Amount = amount,
                    CategoryId = TryReadCategoryId(property.Value)
                });
            }

            return mapped;
        }

        private static bool TryReadAmount(JToken token, out int amount)
        {
            amount = 0;
            if (token == null || token.Type == JTokenType.Null)
            {
                return false;
            }

            if (token.Type == JTokenType.Integer)
            {
                amount = token.Value<int>();
                return true;
            }

            if (token.Type == JTokenType.String)
            {
                return int.TryParse(token.Value<string>(), out amount);
            }

            if (token.Type != JTokenType.Object)
            {
                return false;
            }

            var amountToken = token["amount"] ?? token["Amount"] ?? token["stackCount"] ?? token["StackCount"];
            if (amountToken == null || amountToken.Type == JTokenType.Null)
            {
                return false;
            }

            if (amountToken.Type == JTokenType.Integer)
            {
                amount = amountToken.Value<int>();
                return true;
            }

            if (amountToken.Type == JTokenType.String)
            {
                return int.TryParse(amountToken.Value<string>(), out amount);
            }

            return false;
        }

        private static string TryReadCategoryId(JToken token)
        {
            if (token == null || token.Type != JTokenType.Object)
            {
                return string.Empty;
            }

            return token["categoryId"]?.Value<string>() ??
                   token["CategoryId"]?.Value<string>() ??
                   string.Empty;
        }
    }
}
