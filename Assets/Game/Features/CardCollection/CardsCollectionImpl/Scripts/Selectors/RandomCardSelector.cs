using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public class RandomCardSelector : ICardSelector
    {
        private readonly IPackSelectionStrategy _packSelectionStrategy;

        public RandomCardSelector(IPackSelectionStrategy packSelectionStrategy)
        {
            _packSelectionStrategy = packSelectionStrategy;
        }
        
        public async UniTask<List<string>> SelectCardsAsync(
            CardPack pack,
            List<CardDefinition> allCards,
            string eventId,
            CancellationToken ct = default)
        {
            await UniTask.Yield(ct);

            if (allCards == null || allCards.Count == 0)
            {
                Debug.LogWarning("[RandomCardSelector] No cards available");
                return new List<string>();
            }
            
            var cards = await _packSelectionStrategy.SelectCardsAsync(pack, allCards, ct);
            
            return cards;
        }
    }
}
