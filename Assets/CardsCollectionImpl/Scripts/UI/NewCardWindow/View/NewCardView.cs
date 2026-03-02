using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UIShared;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace core
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
        
        private bool _hasDuplicates;
        
        public void CreateNewCards(List<NewCardDisplayData> cardsData)
        {
            _newCardsPool.DisableNonActive();
            _mockCardsPool.DisableNonActive();
            
            _hasDuplicates = cardsData.Any(c => !c.IsNew);
            
            var sortedCards = cardsData.OrderBy(c => c.Config.PremiumCard ? 6 : c.Config.Stars).ToList();
            
            foreach (var cardDisplayData in sortedCards)
            {
                var cardView = _newCardsPool.GetNext();
                
                var config = cardDisplayData.Config;

                cardView.SetConfig(config);
                cardView.SetCardOpen(cardDisplayData.IsUnlocked);
                cardView.SetCardNew(cardDisplayData.IsNew);
                cardView.UpdateCardName();
                cardView.UpdateCardStars();
                UIUtils.SetSprite(config, cardView, this.GetCancellationTokenOnDestroy()).Forget();
                
                cardView.SetAlpha(false);
                _viewsDict[config] = cardView;
                _duplicatePointsDict[config] = cardDisplayData.DuplicatePoints;
                
                var emptyCardView = _mockCardsPool.GetNext();
                emptyCardView.transform.rotation = Quaternion.Euler(0, 0, 0);
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
        }
    }
}
