using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private UIListPool<CardView> _newCardsPool;
        [SerializeField] private UIListPool<EmptyCardView> _mockCardsPool;
        [SerializeField] private UIListPool<CollectionPointView> _pointStarsPool;
        
        private readonly Dictionary<CardConfig, CardView> _viewsDict = new();
        private readonly Dictionary<CardConfig, EmptyCardView> _mockDict = new();
        private readonly Dictionary<CardConfig, int> _duplicatePointsDict = new();
        private readonly List<CardConfig> _orderedConfigs = new();

        private CancellationToken _ct;
        private bool _hasDuplicates;
        private IEventSpriteManager _eventSpriteManager;

        private void Start()
        {
            _ct = this.GetCancellationTokenOnDestroy();
        }

        public void SetSpriteManager(IEventSpriteManager eventSpriteManager)
        {
            _eventSpriteManager = eventSpriteManager;
        }
        
        public void CreateNewCards(string eventId, List<NewCardDisplayData> cardsData)
        {
            Debug.LogWarning("CreateNewCards called");
            _newCardsPool.DisableNonActive();
            _mockCardsPool.DisableNonActive();
            _orderedConfigs.Clear();
            
            _hasDuplicates = cardsData.Any(c => !c.IsNew);
            
            var sortedCards = cardsData
                .OrderByDescending(c => c.Config.premiumCard)
                .ThenByDescending(c => c.Config.stars)
                .ToList();
            
            for (var i = 0; i < sortedCards.Count; i++)
            {
                var cardDisplayData = sortedCards[i];
                var cardView = _newCardsPool.GetNext();
                
                CardConfig config = cardDisplayData.Config;

                cardView.SetConfig(config);
                cardView.SetCardOpen(cardDisplayData.IsUnlocked);
                cardView.SetCardNew(cardDisplayData.IsNew);
                cardView.UpdateCardStars();
                cardView.UpdateCardFrame();
                cardView.UpdateCardName();
                
                var spriteAddress = eventId + "_" + config.icon;
                _eventSpriteManager.BindSpriteFromAtlasAsync(eventId, eventId, spriteAddress, cardView.CardImage, _ct).Forget();
                
                cardView.SetAlpha(false);
                cardView.transform.SetSiblingIndex(i);
                _viewsDict[config] = cardView;
                _duplicatePointsDict[config] = cardDisplayData.DuplicatePoints;
                _orderedConfigs.Add(config);
                
                var emptyCardView = _mockCardsPool.GetNext();
                emptyCardView.UpdateCardFrame(config.premiumCard);
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
                cardView.SetNewCardFXActive(false);
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
