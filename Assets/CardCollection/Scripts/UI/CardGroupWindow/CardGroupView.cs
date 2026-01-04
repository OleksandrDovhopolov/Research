using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace core
{
    public class CardGroupView : WindowView
    {
        [SerializeField] private UIListPool<CollectionCardView> _upperCardsPool;
        [SerializeField] private UIListPool<CollectionCardView> _bottomCardsPool;

        private readonly Dictionary<string, CollectionCardView> _viewsDict = new();

        public void CreateViews(List<CardCollectionConfig> cardsData)
        {
            _upperCardsPool.DisableNonActive();
            _bottomCardsPool.DisableNonActive();

            _viewsDict.Clear();

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
        
                _viewsDict[config.Id] = cardView;
            }
            
            /*foreach (var cardConfig in cardsData)
            {
                var cardView = _upperCardsPool.GetNext();

                cardView.SetCardName(cardConfig.CardName);
                
                _viewsDict.Add(cardConfig.Id, cardView);
            }*/
        }
        
        public async UniTask SetSprites(List<CardCollectionConfig> cardsData)
        {
            var loadTasks = cardsData.Select(async config => {
                try 
                {
                    var sprite = await ProdAddressablesWrapper.LoadAsync<Sprite>(config.Icon);
                    if (_viewsDict.TryGetValue(config.Id, out var view))
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

        public void DisableAll()
        {
            _upperCardsPool.DisableAll();
            _bottomCardsPool.DisableAll();
        }
    }
}