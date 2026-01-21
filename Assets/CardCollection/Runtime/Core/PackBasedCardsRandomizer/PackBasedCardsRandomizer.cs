using System.Collections.Generic;
using CardCollection.Core;
using CardCollection.Core.Selectors;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class PackBasedCardsRandomizer
    {
        private readonly ICardSelector _cardSelector;

        /// <summary>
        /// Creates a pack-based card randomizer with default random selector.
        /// </summary>
        public PackBasedCardsRandomizer() : this(new RandomCardSelector())
        {
        }

        /// <summary>
        /// Creates a pack-based card randomizer with custom selector.
        /// Allows different selection strategies (random, server-based, etc.)
        /// </summary>
        public PackBasedCardsRandomizer(ICardSelector cardSelector)
        {
            _cardSelector = cardSelector ?? throw new System.ArgumentNullException(nameof(cardSelector));
        }

        public async UniTask<List<string>> GetRandomNewCardsAsync(CardPack pack)
        {
            var configStorage = CardCollectionConfigStorage.Instance;
            var allCards = configStorage.Data;

            if (allCards == null || allCards.Count == 0)
            {
                Debug.LogWarning("[PackBasedCardsRandomizer] No cards available in config");
                return new List<string>();
            }

            // Use the card selector to get cards based on the pack
            // The selector handles the selection logic (random, server-based, etc.)
            var result = await _cardSelector.SelectCardsAsync(pack, allCards);

            foreach (var cardId in result)
            {
                Debug.LogWarning($"Debug New Card with ID {cardId}");
            }

            return result;
        }
    }
}
