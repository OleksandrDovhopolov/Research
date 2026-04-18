using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace CardCollectionImpl
{
    public class ServerCardSelector : ICardSelector
    {
        private const string OpenPackUrl = "packs/open";
        
        public async UniTask<List<string>> SelectCardsAsync(
            CardPack pack,
            List<CardDefinition> allCards,
            string eventId,
            CancellationToken ct = default)
        {
            var request = new OpenPackRequest
            {
                PlayerId = ApiConfig.TemporaryPlayerId,
                EventId = eventId,
                PackId = pack.PackId,
                OpenPackRequestId = Guid.NewGuid().ToString("N")
            };

            var newCards = new List<string>();
            
            try 
            {
                var response = await OpenPackAsync(request, ct);
                newCards = response.OpenedCardIds;
            }
            catch (OperationCanceledException) { /* empty */ }
            catch (Exception e) { Debug.LogException(e); }
            
            Debug.LogWarning($"[Debug] ServerCardSelector {request.PlayerId} / {request.OpenPackRequestId}, newCards {newCards.Count}");
            
            return newCards;
        }
        
        public async UniTask<OpenPackResponse> OpenPackAsync(OpenPackRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var payload = JsonConvert.SerializeObject(request);
            using var webRequest = new UnityWebRequest(ApiConfig.BaseUrl + OpenPackUrl, UnityWebRequest.kHttpVerbPOST);
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest().WithCancellation(ct);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(
                    $"Pack open request failed: {webRequest.responseCode}, {webRequest.error}");
            }

            var responseJson = webRequest.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return new OpenPackResponse();
            }

            Debug.LogWarning($"[Debug] responseJson {responseJson}");
            return JsonConvert.DeserializeObject<OpenPackResponse>(responseJson) ?? new OpenPackResponse();
        }
    }
}
