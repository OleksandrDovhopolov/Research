using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public partial class NewCardView : WindowView
    {
        [Space, Space, Header("Base")]
        [SerializeField] private Button _cardOpenButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private UIListPool<CollectionCardView> _newCardsPool;
        [SerializeField] private UIListPool<EmptyCardView> _mockCardsPool;
        [SerializeField] private UIListPool<CollectionPointView> _pointStarsPool;
        
        private readonly Dictionary<CardCollectionConfig, CollectionCardView> _viewsDict = new();
        private readonly Dictionary<CardCollectionConfig, EmptyCardView> _mockDict = new();
        private readonly Dictionary<CardCollectionConfig, int> _duplicatePointsDict = new();
        private readonly List<CardCollectionConfig> _orderedConfigs = new();
        
        private bool _hasDuplicates;
        
        public void CreateNewCards(List<NewCardDisplayData> cardsData)
        {
            _newCardsPool.DisableNonActive();
            _mockCardsPool.DisableNonActive();
            _orderedConfigs.Clear();
            
            _hasDuplicates = cardsData.Any(c => !c.IsNew);
            
            var sortedCards = cardsData
                .OrderByDescending(c => c.Config.PremiumCard)
                .ThenByDescending(c => c.Config.Stars)
                .ToList();
            
            for (var i = 0; i < sortedCards.Count; i++)
            {
                var cardDisplayData = sortedCards[i];
                var cardView = _newCardsPool.GetNext();
                
                var config = cardDisplayData.Config;

                cardView.SetConfig(config);
                cardView.SetCardOpen(cardDisplayData.IsUnlocked);
                cardView.SetCardNew(cardDisplayData.IsNew);
                cardView.UpdateCardFrame();
                cardView.UpdateCardName();
                cardView.UpdateCardStars();
                UIUtils.SetSprite(config, cardView, this.GetCancellationTokenOnDestroy()).Forget();
                
                cardView.SetAlpha(false);
                cardView.transform.SetSiblingIndex(i);
                _viewsDict[config] = cardView;
                _duplicatePointsDict[config] = cardDisplayData.DuplicatePoints;
                _orderedConfigs.Add(config);
                
                var emptyCardView = _mockCardsPool.GetNext();
                emptyCardView.UpdateCardFrame(config.PremiumCard);
                emptyCardView.transform.rotation = Quaternion.Euler(0, 0, 0);
                emptyCardView.transform.SetSiblingIndex(sortedCards.Count - 1 - i);
                _mockDict[config] = emptyCardView;
            }
        }

        public void UpdatePointsAmount(int pointsAmount)
        {
            _cardsCollectionPointsView.SetPointsAmount(pointsAmount);
        }
        
        public void DisableAll()
        {
            foreach (var emptyCardView in _mockDict.Values)
            {
                emptyCardView.transform.localPosition = Vector3.zero;
                emptyCardView.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            
            foreach (var cardView in _viewsDict.Values)
            {
                cardView.transform.rotation = Quaternion.Euler(0, 0, 0);
                cardView.ResetView();
            }
            
            _newCardsPool.DisableAll();
            _mockCardsPool.DisableAll();
            _pointStarsPool.DisableAll();
            
            _viewsDict.Clear();
            _mockDict.Clear();
            _duplicatePointsDict.Clear();
            _orderedConfigs.Clear();
        }
    }
}
