using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        [SerializeField] private UIListPool<CollectionPointView> _pointStarsPool;
        
        [Header("Points Container")]
        [SerializeField] private float _animationDelay = 1f;
        [SerializeField] private CardsCollectionPointsView _cardsCollectionPointsView;
        [SerializeField] private RectTransform _hidedTransform;
        [SerializeField] private RectTransform _showedTransform;

        private readonly Dictionary<CardCollectionConfig, CollectionCardView> _viewsDict = new();
        private readonly Dictionary<CardCollectionConfig, EmptyCardView> _mockDict = new();
        private readonly Dictionary<CardCollectionConfig, int> _duplicatePointsDict = new();
        
        private bool _hasDuplicates;

        private void OnEnable()
        {
            _canvasGroup.alpha = 1;
            _cardsCollectionPointsView.SetPosition(_hidedTransform.position);
        }
        
        public void CreateNewCards(List<NewCardDisplayData> cardsData)
        {
            _newCardsPool.DisableNonActive();
            _newCardsPool.DisableNonActive();
            
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
        
        public async UniTask HideAllCardsAsync(CancellationToken ct)
        {
            if (_hasDuplicates)
            {
                await _cardsCollectionPointsView.ShowAsync(_hidedTransform.position, _showedTransform.position, ct);
            }

            var targetPosition = _cardsCollectionPointsView.transform.position;
            var tasks = new List<UniTask>();

            foreach (var kvp in _viewsDict)
            {
                var config = kvp.Key;
                var cardView = kvp.Value;

                if (cardView.IsNew)
                {
                    tasks.Add(HideNewCardAsync(cardView, ct));
                }
                else
                {
                    var points = _duplicatePointsDict.GetValueOrDefault(config, 0);
                    tasks.Add(HideCardWithPointAsync(cardView, points, targetPosition, ct));
                }
            }

            await UniTask.WhenAll(tasks);

            if (_hasDuplicates)
            {
                await _cardsCollectionPointsView.HideAsync(_showedTransform.position, _hidedTransform.position, ct);
            }

            await UniTask.Delay(500, cancellationToken: ct);
        }

        private async UniTask HideNewCardAsync(CollectionCardView cardView, CancellationToken ct)
        {
            await UniTask.Delay((int)(_animationDelay * 1000), cancellationToken: ct);
            await cardView.Hide().SetLink(gameObject).AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);
        }

        private async UniTask HideCardWithPointAsync(CollectionCardView cardView, int pointsAmount, Vector3 targetPosition, CancellationToken ct)
        {
            var cardPosition = cardView.transform.position;
            await cardView.Hide().SetLink(gameObject).AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);

            var pointView = _pointStarsPool.GetNext();
            pointView.UpdatePointsAmount(pointsAmount);
            await pointView.AnimateToTarget(cardPosition, targetPosition, ct);
            
            _cardsCollectionPointsView.UpdatePointsAmount( pointView.PointsAmount);
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

        public void UpdatePointsAmount(int pointsAmount)
        {
            _cardsCollectionPointsView.SetPointsAmount(pointsAmount);
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