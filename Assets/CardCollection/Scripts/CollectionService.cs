using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace core
{
    public class CollectionService
    {
        private readonly CardCollectionData _cardCollectionData;
        private readonly List<CardData> _cards;
        
        public IReadOnlyList<CardData> Cards => _cards;
        
        public CollectionService()
        {
            var jsonFile = Resources.Load<TextAsset>("cardCollection");
            if (jsonFile == null)
            {
                Debug.LogError("cards.json not found in Resources!");
                return;
            }

            _cards = JsonConvert.DeserializeObject<List<CardData>>(jsonFile.text);

            _cardCollectionData = new CardCollectionData
            {
                CollectionName = "DefaultCollection",
                Cards = _cards
            };
        }

        public void PrintData()
        {
            Debug.Log($"Loaded {_cardCollectionData.Cards.Count} cards!");

            foreach (var cardData in _cardCollectionData.Cards)
            {
                Debug.LogWarning(
                    $"CardId: {cardData.CardId}, " +
                    $"Name: {cardData.CardName}, " +
                    $"Group: {cardData.GroupType}, " +
                    $"Stars: {cardData.Stars}, " +
                    $"Premium: {cardData.PremiumCard}"
                );
            }
        }
    }
}