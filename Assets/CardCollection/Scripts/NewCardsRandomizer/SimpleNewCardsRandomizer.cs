using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class SimpleNewCardsRandomizer : INewCardsRandomizer
    {
        private const int MinCards = 2;
        private const int MaxCards = 6;

        public async UniTask<List<string>> GetRandomNewCardsAsync()
        {
            await UniTask.Yield();

            var configStorage = CardCollectionConfigStorage.Instance;
            var allCards = configStorage.Data;

            if (allCards == null || allCards.Count == 0)
            {
                Debug.LogWarning("No cards available in config");
                return new List<string>();
            }
            
            var randomCardCount = Random.Range(MinCards, MaxCards + 1);
            
            randomCardCount = Mathf.Min(randomCardCount, allCards.Count);

            var shuffledCards = allCards.OrderBy(x => Random.value).Take(randomCardCount);
            var result = shuffledCards.Select(card => card.Id).ToList();
            
            foreach (var cardId in result)
            {
                Debug.LogWarning($"Debug New Card with ID {cardId}");
            }
            
            return result;
        }
    }
}
