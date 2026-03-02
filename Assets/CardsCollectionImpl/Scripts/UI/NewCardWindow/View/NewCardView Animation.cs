using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace CardCollectionImpl
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
                    .SetEase(_animationConfig.CardMoveEase);
                
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
            var tasks = new List<UniTask>();

            foreach (var (config, mockView) in _mockDict)
            {
                if (!_viewsDict.TryGetValue(config, out var targetView) || mockView == null || targetView == null)
                    continue;

                // Real card starts hidden, rotated to 90° Y (edge-on)
                targetView.transform.rotation = Quaternion.Euler(0, 90, 0);
                targetView.SetAlpha(false);
                targetView.OnCardAnimationStarted();

                tasks.Add(AnimateSingleCardFlipAsync(mockView, targetView, ct));
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        private async UniTask AnimateSingleCardFlipAsync(EmptyCardView mock, CollectionCardView card, CancellationToken ct)
        {
            var sequence = DOTween.Sequence();

            // Phase 1: mock rotates 0° → 90° (disappears edge-on)
            sequence.Append(
                mock.transform.DORotate(new Vector3(0f, 90f, 0f), _animationConfig.FlipMockHalfDuration)
                    .SetEase(_animationConfig.FlipMockEase)
            );

            // At the exact moment mock reaches 90°: disable mock, make real card visible
            sequence.AppendCallback(() =>
            {
                mock.gameObject.SetActive(false);
                card.SetAlpha(true);
                card.OnCardAnimationCompleted();
            });

            // Phase 2: real card rotates 90° → 0° (appears)
            sequence.Append(
                card.transform.DORotate(Vector3.zero, _animationConfig.FlipCardHalfDuration)
                    .SetEase(_animationConfig.FlipCardEase)
            );

            sequence.AppendCallback(() =>
            {
                //card.OnCardAnimationCompleted();
            });

            // Phase 3: scale punch for "pop" feel
            if (_animationConfig.FlipScalePunchStrength > 0f)
            {
                sequence.Append(
                    card.transform.DOPunchScale(
                        Vector3.one * _animationConfig.FlipScalePunchStrength,
                        _animationConfig.FlipScalePunchDuration,
                        vibrato: 1, elasticity: 0.5f)
                );
            }

            await sequence.SetLink(gameObject)
                .AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);
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