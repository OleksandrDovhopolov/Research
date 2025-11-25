using System;
using System.Collections.Generic;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardGroupView : WindowView
    {
        [SerializeField] private GameObject _topCardsContainer;
        [SerializeField] private GameObject _botCardsContainer;
        [SerializeField] private CardItemView _cardItemPrefab;

        private List<CardItemView> _cardItemViews = new();
        
        protected override void Awake()
        {
            base.Awake();
        }

        public void Configure(IReadOnlyList<CardModel> cardModels)
        {
            if (cardModels == null)
                throw new ArgumentNullException(nameof(cardModels));

            if (cardModels.Count != 10)
                throw new ArgumentException($"CardModels must contain exactly 10 elements, but got {cardModels.Count}");
            
            _cardItemViews.Clear();
            
            for (var i = 0; i < 5; i++)
            {
                CreateCard(_topCardsContainer.transform, cardModels[i]);
            }

            for (var i = 5; i < 10; i++)
            {
                CreateCard(_botCardsContainer.transform, cardModels[i]);
            }
        }
        
        private void CreateCard(Transform parent, CardModel cardModel)
        {
            if (_cardItemPrefab == null)
            {
                Debug.LogError("CardItemPrefab не назначен!");
                return;
            }

            var newCard = Instantiate(_cardItemPrefab, parent);
            newCard.Configure(cardModel);
            
            _cardItemViews.Add(newCard);
        }

        public void DestroyCards()
        {
            foreach (var cardItemView in _cardItemViews)
            {
                Destroy(cardItemView);
            }
            _cardItemViews.Clear();
        }
    }
}