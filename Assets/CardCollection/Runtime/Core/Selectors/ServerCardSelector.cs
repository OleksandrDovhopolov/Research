using System;
using System.Collections.Generic;
using System.Text;
using core;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace CardCollection.Core.Selectors
{
    /// <summary>
    /// Server-based card selection strategy.
    /// Fetches card selection from server based on pack configuration.
    /// </summary>
    public class ServerCardSelector : ICardSelector
    {
        private const string SelectionEndpoint = "/api/card-selection";
        private readonly string _serverUrl;

        public ServerCardSelector(string serverUrl)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
        }

        public async UniTask<List<string>> SelectCardsAsync(CardPack pack, List<CardCollectionConfig> availableCards)
        {
            if (pack == null) throw new ArgumentNullException(nameof(pack));
            if (availableCards == null || availableCards.Count == 0)
            {
                Debug.LogWarning("[ServerCardSelector] No cards available for selection.");
                return new List<string>();
            }

            if (string.IsNullOrWhiteSpace(_serverUrl))
            {
                Debug.LogWarning("[ServerCardSelector] Server URL not configured, using random selection.");
                var randomSelector = new RandomCardSelector();
                return await randomSelector.SelectCardsAsync(pack, availableCards);
            }

            var selectionUrl = BuildSelectionUrl(_serverUrl);
            var requestPayload = new CardSelectionRequest
            {
                PackId = pack.PackId,
                CardCount = pack.CardCount,
                AvailableCards = availableCards
            };

            try
            {
                var json = JsonConvert.SerializeObject(requestPayload);
                using var request = new UnityWebRequest(selectionUrl, UnityWebRequest.kHttpVerbPOST);
                var bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest().ToUniTask();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[ServerCardSelector] Server selection failed: {request.error}");
                    var randomSelector = new RandomCardSelector();
                    return await randomSelector.SelectCardsAsync(pack, availableCards);
                }

                var responseJson = request.downloadHandler.text;
                var serverIds = ParseSelectionResponse(responseJson);
                var selectedIds = BuildSelection(serverIds, availableCards, pack.CardCount);

                if (selectedIds.Count == 0)
                {
                    Debug.LogWarning("[ServerCardSelector] Server returned no valid selections, using random selection.");
                    var randomSelector = new RandomCardSelector();
                    return await randomSelector.SelectCardsAsync(pack, availableCards);
                }

                return selectedIds;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerCardSelector] Error selecting cards: {ex.Message}");
                var randomSelector = new RandomCardSelector();
                return await randomSelector.SelectCardsAsync(pack, availableCards);
            }
        }

        private static string BuildSelectionUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return string.Empty;
            }

            if (baseUrl.EndsWith(SelectionEndpoint, StringComparison.OrdinalIgnoreCase))
            {
                return baseUrl;
            }

            return baseUrl.EndsWith("/", StringComparison.Ordinal)
                ? $"{baseUrl.TrimEnd('/')}{SelectionEndpoint}"
                : $"{baseUrl}{SelectionEndpoint}";
        }

        private static List<string> ParseSelectionResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                var response = JsonConvert.DeserializeObject<CardSelectionResponse>(json);
                if (response?.CardIds != null && response.CardIds.Count > 0)
                {
                    return response.CardIds;
                }

                if (response?.SelectedCardIds != null && response.SelectedCardIds.Count > 0)
                {
                    return response.SelectedCardIds;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ServerCardSelector] Failed to parse response object: {ex.Message}");
            }

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ServerCardSelector] Failed to parse response list: {ex.Message}");
                return new List<string>();
            }
        }

        private static List<string> BuildSelection(
            List<string> serverIds,
            List<CardCollectionConfig> availableCards,
            int cardCount)
        {
            var availableIds = new HashSet<string>();
            foreach (var card in availableCards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.Id))
                {
                    continue;
                }

                availableIds.Add(card.Id);
            }

            var maxCount = Mathf.Clamp(cardCount, 0, availableIds.Count);
            if (maxCount == 0)
            {
                return new List<string>();
            }

            var selectedIds = new List<string>(maxCount);
            var selectedSet = new HashSet<string>();

            if (serverIds != null)
            {
                foreach (var id in serverIds)
                {
                    if (string.IsNullOrWhiteSpace(id) || !availableIds.Contains(id))
                    {
                        continue;
                    }

                    if (selectedSet.Add(id))
                    {
                        selectedIds.Add(id);
                        if (selectedIds.Count >= maxCount)
                        {
                            return selectedIds;
                        }
                    }
                }
            }

            if (selectedIds.Count >= maxCount)
            {
                return selectedIds;
            }

            var remaining = new List<string>();
            foreach (var card in availableCards)
            {
                var id = card?.Id;
                if (string.IsNullOrWhiteSpace(id) || selectedSet.Contains(id))
                {
                    continue;
                }

                remaining.Add(id);
            }

            Shuffle(remaining);

            var needed = maxCount - selectedIds.Count;
            for (var i = 0; i < needed && i < remaining.Count; i++)
            {
                selectedIds.Add(remaining[i]);
            }

            return selectedIds;
        }

        private static void Shuffle(List<string> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                var temp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = temp;
            }
        }

        [Serializable]
        private sealed class CardSelectionRequest
        {
            [JsonProperty("packId")]
            public string PackId;

            [JsonProperty("cardCount")]
            public int CardCount;

            [JsonProperty("availableCards")]
            public List<CardCollectionConfig> AvailableCards;
        }

        [Serializable]
        private sealed class CardSelectionResponse
        {
            [JsonProperty("cardIds")]
            public List<string> CardIds = new();

            [JsonProperty("selectedCardIds")]
            public List<string> SelectedCardIds = new();
        }
    }
}
