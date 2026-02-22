using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace core
{
    public partial class NewCardView
    {
        [Header("Points Container")]
        [SerializeField] private CardsCollectionPointsView _cardsCollectionPointsView;
        [SerializeField] private RectTransform _hidedTransform;
        [SerializeField] private RectTransform _showedTransform;
        [SerializeField] private NewCardAnimationConfig _animationConfig;
        
        private void OnEnable()
        {
            _canvasGroup.alpha = 1;
            _cardsCollectionPointsView.SetPosition(_hidedTransform.position);
        }
        
        public UniTask PlayOpenSequenceAsync(CancellationToken ct)
        {
            return new AnimationSequenceBuilder()
                .Append(AnimateMocksToPositionsAsync)
                .Append(AnimateCardFlipsAsync)
                .PlayAsync(ct);
        }

        public UniTask PlayCloseSequenceAsync(CancellationToken ct)
        {
            return new AnimationSequenceBuilder()
                .AppendIf(_hasDuplicates, AnimateShowPointsViewAsync)
                .Append(AnimateHideAllCardsAsync)
                .AppendIf(_hasDuplicates, AnimateHidePointsViewAsync)
                .AppendDelay(_animationConfig.WindowCloseDelayMilliseconds)
                .PlayAsync(ct);
        }
        
        private async UniTask AnimateMocksToPositionsAsync(CancellationToken ct)
        {
            var moveSequence = DOTween.Sequence();

            foreach (var (config, mockView) in _mockDict)
            {
                if (!_viewsDict.TryGetValue(config, out var targetView) || mockView == null || targetView == null)
                    continue;

                var tween = mockView.transform.DOMove(targetView.transform.position, _animationConfig.CardMoveDuration)
                    .SetEase(Ease.InOutQuad);
                
                moveSequence.Join(tween);
            }

            if (moveSequence.Duration() > 0)
            {
                await moveSequence.SetLink(gameObject).AsyncWaitForCompletion().AsUniTask()
                    .AttachExternalCancellation(ct);
            }
        }
        
        private async UniTask AnimateCardFlipsAsync(CancellationToken ct)
        {
            foreach (var cardView in _viewsDict.Values)
            {
                cardView.transform.rotation = Quaternion.Euler(0, 90, 0);
                cardView.SetAlpha(true);
            }
            
            var animationDuration = _animationConfig.FlipDuration;
            var tweens = new List<Tween>();

            foreach (var (config, mockView) in _mockDict)
            {
                if (!_viewsDict.TryGetValue(config, out var targetView) || mockView == null || targetView == null)
                    continue;

                var mockRotateTween = mockView.transform.DORotate(new Vector3(0f, 90f, 0f), animationDuration)
                    .SetEase(Ease.InOutQuad);
                tweens.Add(mockRotateTween);

                var cardRotateTween = targetView.transform.DORotate(new Vector3(0f, 0f, 0f), animationDuration)
                    .SetDelay(animationDuration / 2)
                    .SetEase(Ease.InOutQuad);
                tweens.Add(cardRotateTween);
            }
            
            if (tweens.Count > 0)
            {
                await UniTask.WhenAll(tweens.Select(tween => tween.AsyncWaitForCompletion().AsUniTask()));
            }
        }
        
        private UniTask AnimateShowPointsViewAsync(CancellationToken ct)
        {
            return _cardsCollectionPointsView.ShowAsync(_hidedTransform.position, _showedTransform.position, ct);
        }
        
        private async UniTask AnimateHideAllCardsAsync(CancellationToken ct)
        {
            var targetPosition = _cardsCollectionPointsView.transform.position;
            var tasks = new List<UniTask>();

            foreach (var (config, cardView) in _viewsDict)
            {
                if (cardView.IsNew)
                {
                    tasks.Add(AnimateHideNewCardAsync(cardView, ct));
                }
                else
                {
                    var points = _duplicatePointsDict.GetValueOrDefault(config, 0);
                    tasks.Add(AnimateHideCardWithPointAsync(cardView, points, targetPosition, ct));
                }
            }

            await UniTask.WhenAll(tasks);
        }
        
        private UniTask AnimateHidePointsViewAsync(CancellationToken ct)
        {
            return _cardsCollectionPointsView.HideAsync(_showedTransform.position, _hidedTransform.position, ct);
        }

        private async UniTask AnimateHideNewCardAsync(CollectionCardView cardView, CancellationToken ct)
        {
            await UniTask.Delay((int)(_animationConfig.NewCardHideDelay * 1000), cancellationToken: ct);
            await cardView.Hide().SetLink(gameObject).AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);
        }

        private async UniTask AnimateHideCardWithPointAsync(CollectionCardView cardView, int pointsAmount, Vector3 targetPosition, CancellationToken ct)
        {
            var cardPosition = cardView.transform.position;
            await cardView.Hide().SetLink(gameObject).AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);

            var pointView = _pointStarsPool.GetNext();
            pointView.UpdatePointsAmount(pointsAmount);
            await pointView.AnimateToTarget(cardPosition, targetPosition, ct);
            
            _cardsCollectionPointsView.UpdatePointsAmount(pointView.PointsAmount);
        }
    }
}