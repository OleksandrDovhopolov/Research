using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardGroupView : WindowView
    {
        [Space, Space, Header("CardsPool")]
        [SerializeField] private UIListPool<CollectionCardView> _upperCardsPool;
        [SerializeField] private UIListPool<CollectionCardView> _bottomCardsPool;
        [SerializeField] private SelectedCardView _selectedCardAnimation;

        private readonly Dictionary<CardCollectionConfig, CollectionCardView> _viewsDict = new();

        public void CreateViews(List<CardCollectionConfig> cardsData)
        {
            _upperCardsPool.DisableNonActive();
            _bottomCardsPool.DisableNonActive();
            
            for (var i = 0; i < cardsData.Count; i++)
            {
                if (i >= 10)
                {
                    Debug.LogError($"More than 10 elements");
                    break;
                }
                
                var config = cardsData[i];
                var pool = i < 5 ? _upperCardsPool : _bottomCardsPool;
        
                var cardView = pool.GetNext();
                cardView.SetCardName(config.CardName);
                cardView.SetStars(config.Stars);
                cardView.OnCardPressed += OnCardPressedHandler;
        
                _viewsDict[config] = cardView;
            }
        }
        
        public async UniTask SetSprites(List<CardCollectionConfig> cardsData)
        {
            var loadTasks = cardsData.Select(async config => {
                try 
                {
                    var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.Icon);
                    if (_viewsDict.TryGetValue(config, out var view))
                        view.SetCardImage(sprite);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed sprite {config.CardName}: {e}");
                }
            });
                
            await UniTask.WhenAll(loadTasks);
            await UniTask.WaitForSeconds(2f);
        }

        private void OnCardPressedHandler(CollectionCardView cardView)
        {
            CardCollectionConfig config = null;
            foreach (var pair in _viewsDict)
            {
                if (ReferenceEquals(pair.Value, cardView))
                {
                    config = pair.Key;
                }
            }

            if (config == null) return;

            _selectedCardAnimation.OnCardPressedHandler(cardView, config);
        }

        public void DisableAll()
        {
            foreach (var cardView in _viewsDict.Values)
            {
                cardView.OnCardPressed -= OnCardPressedHandler;
            }
            _viewsDict.Clear();
            
            _upperCardsPool.DisableAll();
            _bottomCardsPool.DisableAll();
        }
    }
}