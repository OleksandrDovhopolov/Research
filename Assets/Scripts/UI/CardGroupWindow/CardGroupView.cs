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
        
        public void CreateDataViews(string groupType, List<CardProgressData> cardsData)
        {
            var configs = CardCollectionConfigStorage.Instance.Get(groupType);
            
            _upperCardsPool.DisableNonActive();
            _bottomCardsPool.DisableNonActive();
            
            for (var i = 0; i < cardsData.Count; i++)
            {
                if (i >= 10)
                {
                    Debug.LogError($"More than 10 elements");
                    break;
                }
                
                var data = cardsData[i];
                var config = configs.Find(config => config.Id == data.CardId);
                if (config == null)
                {
                    Debug.LogError($"Debug. GroupId {groupType}. failed to Find config.ID ==  data.CardId {data.CardId}");
                    continue;
                }
                var pool = i < 5 ? _upperCardsPool : _bottomCardsPool;
        
                var cardView = pool.GetNext();
                cardView.SetCardOpen(data.IsUnlocked);
                cardView.SetCardNew(data.IsNew);
                cardView.SetCardName(config.CardName);
                cardView.SetStars(config.Stars);
                cardView.OnCardPressed += OnCardPressedHandler;
        
                _viewsDict[config] = cardView;
            }
        }
        
        public async UniTask SetSprites(List<CardCollectionConfig> cardsData)
        {
            await UIUtils.LoadAndSetSpritesAsync(
                cardsData,
                config => config.Icon,
                config => _viewsDict.TryGetValue(config, out var view) ? view : null,
                (view, sprite) => view.SetCardImage(sprite),
                config => config.CardName);
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