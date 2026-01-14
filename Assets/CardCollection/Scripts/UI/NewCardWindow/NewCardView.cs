using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        [SerializeField] private UIListPool<EmptyCardView> _mockCardsPool;

        private readonly Dictionary<CardCollectionConfig, CollectionCardView> _viewsDict = new();
        private readonly Dictionary<CardCollectionConfig, EmptyCardView> _mockDict = new();
        
        private void OnEnable()
        {
            _canvasGroup.alpha = 1;
        }
        
        public void CreateNewCards(List<NewCardDisplayData> cardsData)
        {
            _newCardsPool.DisableNonActive();
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
                
                cardView.SetAlpha(false);
                _viewsDict[config] = cardView;
            }

            for (var i = 0; i < cardsData.Count; i++)
            {
                var cardView = _mockCardsPool.GetNext();
                var data = cardsData[i];
                var config = data.Config;
                
                _mockDict[config] = cardView;
            }
        }
        
        public void DisableAll()
        {
            foreach (var emptyCardView in _mockDict.Values)
            {
                emptyCardView.transform.localPosition = Vector3.zero;
            }
            
            _newCardsPool.DisableAll();
            _mockCardsPool.DisableAll();
        }

        public async UniTaskVoid CreateMocks()
        {
            Debug.LogWarning("Animate mock cards to new card positions");

            var sequence = DOTween.Sequence();

            foreach (var kvp in _mockDict)
            {
                var config = kvp.Key;
                var mockView = kvp.Value;

                if (!_viewsDict.TryGetValue(config, out var targetView) || mockView == null || targetView == null)
                    continue;

                var tween = mockView.transform.DOMove(targetView.transform.position, 0.5f)
                    .SetEase(Ease.InOutQuad);
                
                sequence.Join(tween);
            }

            if (sequence.Duration() > 0)
            {
                await sequence.AsyncWaitForCompletion().AsUniTask();
            }

            foreach (var cardView in _viewsDict.Values)
            {
                cardView.SetAlpha(true);
            }
        }
    }
}