using System.Collections.Generic;
using System.Linq;
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
            
            var sortedCards = cardsData.OrderBy(c => c.Config.PremiumCard ? 6 : c.Config.Stars).ToList();
            
            foreach (var cardDisplayData in sortedCards)
            {
                var cardView = _newCardsPool.GetNext();
                
                var config = cardDisplayData.Config;
                
                cardView.SetCardOpen(cardDisplayData.IsUnlocked);
                cardView.SetCardNew(cardDisplayData.IsNew);
                cardView.SetCardName(config.CardName);
                cardView.SetStars(config.Stars);
                UIUtils.SetSprite(config, cardView, this.GetCancellationTokenOnDestroy()).Forget();
                
                cardView.SetAlpha(false);
                _viewsDict[config] = cardView;
                
                var emptyCardView = _mockCardsPool.GetNext();
                emptyCardView.transform.rotation = Quaternion.Euler(0, 0, 0);
                _mockDict[config] = emptyCardView;
            }
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
            }
            
            _newCardsPool.DisableAll();
            _mockCardsPool.DisableAll();
        }

        public async UniTaskVoid CreateMocks()
        {
            Debug.LogWarning("Animate mock cards to new card positions");

            var moveSequence = DOTween.Sequence();

            foreach (var kvp in _mockDict)
            {
                var config = kvp.Key;
                var mockView = kvp.Value;

                if (!_viewsDict.TryGetValue(config, out var targetView) || mockView == null || targetView == null)
                    continue;

                var tween = mockView.transform.DOMove(targetView.transform.position, 0.5f)
                    .SetEase(Ease.InOutQuad);
                
                moveSequence.Join(tween);
            }

            if (moveSequence.Duration() > 0)
            {
                await moveSequence.AsyncWaitForCompletion().AsUniTask();
            }
            
            foreach (var cardView in _viewsDict.Values)
            {
                cardView.transform.rotation = Quaternion.Euler(0, 90, 0);
                cardView.SetAlpha(true);
            }
            
            var animationDuration = 0.5f;
            var sequence = new List<Tween>();
            foreach (var kvp in _mockDict)
            {
                var config = kvp.Key;
                var mockView = kvp.Value;

                if (!_viewsDict.TryGetValue(config, out var targetView) || mockView == null || targetView == null)
                    continue;

                var mockRotateTween = mockView.transform.DORotate(new Vector3(0f, 90f, 0f), animationDuration)
                    .SetEase(Ease.InOutQuad);

                sequence.Add(mockRotateTween);

                var cardRotateTween = targetView.transform.DORotate(new Vector3(0f, 0f, 0f), animationDuration)
                    .SetDelay(animationDuration / 2)
                    .SetEase(Ease.InOutQuad);

                sequence.Add(cardRotateTween);
            }
            
            if (sequence.Count > 0)
            {
                await UniTask.WhenAll(sequence.Select(tween => tween.AsyncWaitForCompletion().AsUniTask()));
            }
        }
    }
}