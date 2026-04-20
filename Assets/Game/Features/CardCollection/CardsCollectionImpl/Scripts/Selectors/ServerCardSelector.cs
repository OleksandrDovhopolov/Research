using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace CardCollectionImpl
{
    public class ServerCardSelector : ICardSelector
    {
        private const string OpenPackUrl = "packs/open";
        private readonly IWebClient _webClient;

        public ServerCardSelector(IWebClient webClient)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }
        
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

            var response = await _webClient.PostAsync<OpenPackRequest, OpenPackResponse>(OpenPackUrl, request, ct);
            if (response == null)
            {
                return new OpenPackResponse();
            }

            return response;
        }
    }
}
