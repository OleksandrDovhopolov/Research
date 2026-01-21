using System;
using System.Collections.Generic;
using core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core.Selectors
{
    /// <summary>
    /// Server-based card selection strategy.
    /// Fetches card selection from server based on pack configuration.
    /// </summary>
    public class ServerCardSelector : ICardSelector
    {
        private readonly string _serverUrl;

        public ServerCardSelector(string serverUrl)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
        }

        public async UniTask<List<string>> SelectCardsAsync(CardPack pack, List<CardCollectionConfig> availableCards)
        {
            // TODO: Implement server-based card selection
            // Example implementation:
            // 1. Send pack ID and available cards to server
            // 2. Server returns selected card IDs based on:
            //    - Pack rarity rules
            //    - User's collection state
            //    - Server-side algorithms
            //    - etc.
            // 3. Return server-selected card IDs

            Debug.LogWarning($"[ServerCardSelector] Server-based selection not yet implemented for pack: {pack.PackId}");
            
            // Fallback to random selection for now
            var randomSelector = new RandomCardSelector();
            return await randomSelector.SelectCardsAsync(pack, availableCards);
        }
    }
}
