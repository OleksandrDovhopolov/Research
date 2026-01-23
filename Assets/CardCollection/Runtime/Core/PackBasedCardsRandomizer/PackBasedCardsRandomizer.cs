using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    public class PackBasedCardsRandomizer
    {
        private readonly ICardSelector _cardSelector;
        private readonly ICardDefinitionProvider _cardDefinitionProvider;
        
        public PackBasedCardsRandomizer(ICardSelector cardSelector, ICardDefinitionProvider cardDefinitionProvider)
        {
            _cardSelector = cardSelector ?? throw new ArgumentNullException(nameof(cardSelector));
            _cardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
        }

        public async UniTask<List<string>> GetRandomNewCardsAsync(CardPack pack)
        {
            var allCards = _cardDefinitionProvider.GetCardDefinitions();

            if (allCards == null || allCards.Count == 0)
            {
                Debug.LogWarning("[PackBasedCardsRandomizer] No cards available in config");
                return new List<string>();
            }

            var result = await _cardSelector.SelectCardsAsync(pack, allCards);

            return result;
        }
    }
}
