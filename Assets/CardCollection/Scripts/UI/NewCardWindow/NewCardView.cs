using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class NewCardView : WindowView
    {
        [SerializeField] private Button _cardOpenButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private UIListPool<CollectionCardView> _newCardsPool;

        private readonly Dictionary<CardCollectionConfig, CollectionCardView> _viewsDict = new();
        
        private void OnEnable()
        {
            _canvasGroup.alpha = 1;
        }
        
        public void CreateNewCards(List<NewCardDisplayData> cardsData)
        {
            _newCardsPool.DisableNonActive();

            for (var i = 0; i < cardsData.Count; i++)
            {
                var cardView = _newCardsPool.GetNext();
                
                var data = cardsData[i];
                var config = data.Config;
                
                cardView.SetCardOpen(data.IsUnlocked);
                cardView.SetCardNew(data.IsNew);
                cardView.SetCardName(config.CardName);
                cardView.SetStars(config.Stars);
                UIUtils.SetSprite(config, cardView, this.GetCancellationTokenOnDestroy()).Forget();
                
                _viewsDict[config] = cardView;
            }
        }
        
        public void DisableAll()
        {
            _newCardsPool.DisableAll();
        }
    }
}