using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace UIShared.AnimationTransitionService
{
    public class TransitionAnimationService : MonoBehaviour
    {
        [SerializeField] private RectTransform _leftContainerRectTransform;
        [SerializeField] private RectTransform _rightContainerRectTransform;
        [SerializeField] private float _coverDurationSeconds = 0.35f;
        [SerializeField] private float _revealDurationSeconds = 0.35f;
        [SerializeField] private Ease _coverEase = Ease.InOutQuad;
        [SerializeField] private Ease _revealEase = Ease.InOutQuad;

        private Vector2 _leftStartPosition;
        private Vector2 _rightStartPosition;
        private bool _hasCachedStartPositions;
        private Sequence _activeSequence;

        private void Start()
        {
            CacheStartPositions();
        }

        public async UniTask PlayCoverAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureStartPositionsCached();

            KillActiveSequence();

            _activeSequence = DOTween.Sequence();
            if (_leftContainerRectTransform != null)
            {
                _activeSequence.Join(
                    _leftContainerRectTransform
                        .DOAnchorPos(Vector2.zero, _coverDurationSeconds)
                        .SetEase(_coverEase));
            }

            if (_rightContainerRectTransform != null)
            {
                _activeSequence.Join(
                    _rightContainerRectTransform
                        .DOAnchorPos(Vector2.zero, _coverDurationSeconds)
                        .SetEase(_coverEase));
            }

            await AwaitTweenAsync(_activeSequence, ct);
        }

        public async UniTask PlayRevealAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            EnsureStartPositionsCached();

            KillActiveSequence();

            _activeSequence = DOTween.Sequence();
            if (_leftContainerRectTransform != null)
            {
                _activeSequence.Join(
                    _leftContainerRectTransform
                        .DOAnchorPos(_leftStartPosition, _revealDurationSeconds)
                        .SetEase(_revealEase));
            }

            if (_rightContainerRectTransform != null)
            {
                _activeSequence.Join(
                    _rightContainerRectTransform
                        .DOAnchorPos(_rightStartPosition, _revealDurationSeconds)
                        .SetEase(_revealEase));
            }

            await AwaitTweenAsync(_activeSequence, ct);
        }

        private void OnDestroy()
        {
            KillActiveSequence();
        }

        private void CacheStartPositions()
        {
            _leftStartPosition = _leftContainerRectTransform != null
                ? _leftContainerRectTransform.anchoredPosition
                : Vector2.zero;

            _rightStartPosition = _rightContainerRectTransform != null
                ? _rightContainerRectTransform.anchoredPosition
                : Vector2.zero;

            _hasCachedStartPositions = true;
        }

        private void EnsureStartPositionsCached()
        {
            if (_hasCachedStartPositions)
            {
                return;
            }

            CacheStartPositions();
        }

        private static async UniTask AwaitTweenAsync(Tween tween, CancellationToken ct)
        {
            if (tween == null)
            {
                return;
            }

            ct.ThrowIfCancellationRequested();
            var tcs = new UniTaskCompletionSource();
            var completed = false;

            tween.OnComplete(() =>
            {
                completed = true;
                tcs.TrySetResult();
            });

            tween.OnKill(() =>
            {
                if (completed)
                {
                    return;
                }

                tcs.TrySetCanceled();
            });

            await using var registration = ct.Register(() =>
            {
                if (tween.IsActive())
                {
                    tween.Kill();
                }
            });

            await tcs.Task.AttachExternalCancellation(ct);
        }

        private void KillActiveSequence()
        {
            if (_activeSequence == null)
            {
                return;
            }

            if (_activeSequence.IsActive())
            {
                _activeSequence.Kill();
            }

            _activeSequence = null;
        }
    }
}