using System.Collections.Generic;
using System.Linq;
using core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    /// <summary>
    /// Simple random card selection strategy.
    /// Selects random cards from available cards based on pack's CardCount.
    /// </summary>
    public class RandomCardSelector : ICardSelector
    {
        public async UniTask<List<string>> SelectCardsAsync(CardPack pack, List<CardCollectionConfig> availableCards)
        {
            await UniTask.Yield();

            if (availableCards == null || availableCards.Count == 0)
            {
                Debug.LogWarning("[RandomCardSelector] No cards available");
                return new List<string>();
            }

            var cardCount = pack.CardCount;
            cardCount = Mathf.Min(cardCount, availableCards.Count);

            var shuffledCards = availableCards.OrderBy(x => Random.value).Take(cardCount);
            var result = shuffledCards.Select(card => card.Id).ToList();

            return result;
        }
    }
}
