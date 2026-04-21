using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Rewards
{
    //TODO refactor. the same GET logic in HttpSaveStorage
    public sealed class ServerRewardPlayerStateSyncService : IRewardPlayerStateSyncService
    {
        private const string SaveGlobalPath = "save/global";
        private readonly IPlayerIdentityProvider _playerIdentityProvider;
        private readonly IWebClient _webClient;
        private readonly IReadOnlyList<IPlayerStateSnapshotHandler> _snapshotHandlers;

        public ServerRewardPlayerStateSyncService(
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

        public async UniTask SyncFromGlobalSaveAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var playerId = _playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new InvalidOperationException("Player id is empty.");
            }

            var requestUrl = $"{SaveGlobalPath}?playerId={Uri.EscapeDataString(playerId)}";
            Debug.Log($"[AdsRewardFlow] Save/global sync started. Url={requestUrl}");
            var response = await _webClient.GetAsync<JToken>(requestUrl, ct);
            if (response == null || response.Type is JTokenType.Null or JTokenType.Undefined)
            {
                throw new InvalidOperationException("Save/global response is empty.");
            }

            var payload = ExtractPayload(response);
            if (payload is not JObject payloadObject)
            {
                throw new InvalidOperationException("Save/global payload is not a JSON object.");
            }

            var snapshot = BuildSnapshot(payloadObject);
            if ((snapshot.Resources == null || snapshot.Resources.Count == 0) &&
                (snapshot.InventoryItems == null || snapshot.InventoryItems.Count == 0))
            {
                throw new InvalidOperationException("Save/global payload does not contain resources or inventory items.");
            }

            foreach (var snapshotHandler in _snapshotHandlers)
            {
                ct.ThrowIfCancellationRequested();
                await snapshotHandler.ApplyAsync(snapshot, ct);
            }

            Debug.Log($"[AdsRewardFlow] Save/global sync success. Resources={snapshot.Resources?.Count ?? 0}, InventoryItems={snapshot.InventoryItems?.Count ?? 0}");
        }

        private static JToken ExtractPayload(JToken response)
        {
            if (response.Type == JTokenType.String)
            {
                var rawRoot = response.Value<string>();
                if (string.IsNullOrWhiteSpace(rawRoot))
                {
                    throw new InvalidOperationException("Save/global response root string is empty.");
                }

                try
                {
                    return JToken.Parse(rawRoot);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Save/global root string is not valid JSON. {exception.Message}", exception);
                }
            }

            if (response is not JObject root)
            {
                return response;
            }

            if (!root.TryGetValue("data", StringComparison.OrdinalIgnoreCase, out var dataToken))
            {
                return response;
            }

            if (dataToken == null || dataToken.Type is JTokenType.Null or JTokenType.Undefined)
            {
                throw new InvalidOperationException("Save/global envelope contains empty data field.");
            }

            if (dataToken.Type == JTokenType.String)
            {
                var rawData = dataToken.Value<string>();
                if (string.IsNullOrWhiteSpace(rawData))
                {
                    throw new InvalidOperationException("Save/global envelope contains blank data string.");
                }

                try
                {
                    return JToken.Parse(rawData);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Save/global data string is not valid JSON. {exception.Message}", exception);
                }
            }

            return dataToken;
        }

        private static PlayerStateSnapshotDto BuildSnapshot(JObject payloadObject)
        {
            var snapshotRoot = payloadObject;
            if (payloadObject.TryGetValue("playerState", StringComparison.OrdinalIgnoreCase, out var playerStateToken) &&
                playerStateToken is JObject playerStateObject)
            {
                snapshotRoot = playerStateObject;
            }

            var snapshot = new PlayerStateSnapshotDto();
            if (snapshotRoot.TryGetValue("resources", StringComparison.OrdinalIgnoreCase, out var resourcesToken) &&
                resourcesToken is JObject resourcesObject)
            {
                foreach (var property in resourcesObject.Properties())
                {
                    if (string.IsNullOrWhiteSpace(property.Name) || !TryReadInt(property.Value, out var amount))
                    {
                        continue;
                    }

                    snapshot.Resources[property.Name] = Math.Max(0, amount);
                }
            }

            if (TryGetInventoryItemsToken(snapshotRoot, out var inventoryItemsToken))
            {
                FillInventoryItems(snapshot.InventoryItems, inventoryItemsToken);
            }

            return snapshot;
        }

        private static bool TryGetInventoryItemsToken(JObject snapshotRoot, out JToken inventoryItemsToken)
        {
            if (snapshotRoot.TryGetValue("inventoryItems", StringComparison.OrdinalIgnoreCase, out inventoryItemsToken))
            {
                return true;
            }

            if (snapshotRoot.TryGetValue("inventory", StringComparison.OrdinalIgnoreCase, out var inventoryToken) &&
                inventoryToken is JObject inventoryObject &&
                inventoryObject.TryGetValue("inventoryItems", StringComparison.OrdinalIgnoreCase, out inventoryItemsToken))
            {
                return true;
            }

            inventoryItemsToken = null;
            return false;
        }

        private static void FillInventoryItems(List<InventoryItemDto> output, JToken inventoryItemsToken)
        {
            if (output == null || inventoryItemsToken == null || inventoryItemsToken.Type is JTokenType.Null or JTokenType.Undefined)
            {
                return;
            }

            if (inventoryItemsToken is JArray itemsArray)
            {
                foreach (var itemToken in itemsArray)
                {
                    if (itemToken is not JObject itemObject)
                    {
                        continue;
                    }

                    if (!itemObject.TryGetValue("itemId", StringComparison.OrdinalIgnoreCase, out var itemIdToken))
                    {
                        continue;
                    }

                    var itemId = itemIdToken?.Value<string>();
                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        continue;
                    }

                    if (!TryReadInventoryAmount(itemObject, out var amount) || amount <= 0)
                    {
                        continue;
                    }

                    output.Add(new InventoryItemDto
                    {
                        ItemId = itemId,
                        Amount = amount
                    });
                }

                return;
            }

            if (inventoryItemsToken is not JObject itemsObject)
            {
                return;
            }

            foreach (var property in itemsObject.Properties())
            {
                if (string.IsNullOrWhiteSpace(property.Name) || !TryReadInventoryAmount(property.Value, out var amount) || amount <= 0)
                {
                    continue;
                }

                output.Add(new InventoryItemDto
                {
                    ItemId = property.Name,
                    Amount = amount
                });
            }
        }

        private static bool TryReadInventoryAmount(JObject itemObject, out int amount)
        {
            amount = 0;
            if (itemObject == null)
            {
                return false;
            }

            if (itemObject.TryGetValue("amount", StringComparison.OrdinalIgnoreCase, out var amountToken) &&
                TryReadInventoryAmount(amountToken, out amount))
            {
                return true;
            }

            if (itemObject.TryGetValue("stackCount", StringComparison.OrdinalIgnoreCase, out var stackCountToken) &&
                TryReadInventoryAmount(stackCountToken, out amount))
            {
                return true;
            }

            return false;
        }

        private static bool TryReadInventoryAmount(JToken token, out int amount)
        {
            if (TryReadInt(token, out amount))
            {
                return true;
            }

            if (token is not JObject tokenObject)
            {
                return false;
            }

            return TryReadInventoryAmount(tokenObject, out amount);
        }

        private static bool TryReadInt(JToken token, out int value)
        {
            value = 0;
            if (token == null || token.Type is JTokenType.Null or JTokenType.Undefined)
            {
                return false;
            }

            switch (token.Type)
            {
                case JTokenType.Integer:
                    value = token.Value<int>();
                    return true;
                case JTokenType.Float:
                    value = (int)token.Value<float>();
                    return true;
                case JTokenType.String:
                    return int.TryParse(token.Value<string>(), out value);
                default:
                    return false;
            }
        }
    }
}
