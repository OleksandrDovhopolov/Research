using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace core
{
    public class CollectionService
    {
        private CardCollectionData _cardCollectionData;
        
        public CollectionService()
        {
            LoadCardCollection();
        }
        
        private void LoadCardCollection()
        {
            var jsonFile = Resources.Load<TextAsset>("cardCollection");
            if (jsonFile == null)
            {
                Debug.LogError("cards.json not found in Resources!");
                return;
            }

            var cards = JsonConvert.DeserializeObject<List<CardData>>(jsonFile.text);

            _cardCollectionData = new CardCollectionData
            {
                CollectionName = "DefaultCollection",
                Cards = cards
            };
        }

        public void PrintData()
        {
            Debug.Log($"Loaded {_cardCollectionData.Cards.Count} cards!");

            foreach (var cardData in _cardCollectionData.Cards)
            {
                Debug.LogWarning($"CardId: {cardData.CardId}, " + $"Name: {cardData.CardName}, " +
                                 $"Group: {cardData.GroupType}, " + $"Stars: {cardData.Stars}, " +
                                 $"Premium: {cardData.PremiumCard}");
            }
        }
    }
}