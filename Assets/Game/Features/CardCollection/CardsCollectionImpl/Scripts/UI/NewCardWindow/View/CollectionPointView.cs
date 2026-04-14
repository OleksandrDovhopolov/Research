using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CollectionPointView : MonoBehaviour
    {
        [SerializeField] private NewCardAnimationConfig _animationConfig;
        [SerializeField] private TextMeshProUGUI _pointAmount;
        [SerializeField] private RectTransform _container;

        public int PointsAmount { get; private set; }

        public void UpdatePointsAmount(int pointsAmount)
        {
            PointsAmount = pointsAmount;
            _pointAmount.text = PointsAmount.ToString();
        }

        public async UniTask AnimateToTarget(Vector3 fromPosition, Vector3 targetPosition, CancellationToken ct)
        {
            transform.position = fromPosition;
            transform.localScale = Vector3.zero;
            _pointAmount.alpha = 1f;
            _container.localRotation = Quaternion.identity;

            var moveDuration = _animationConfig.PointViewMoveDuration;
            var rotationAngle = _animationConfig.PointViewMoveRotationSpeed * moveDuration;
            var fadeDuration = moveDuration * _animationConfig.PointViewFadeDurationRatio;

            var sequence = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one, _animationConfig.PointViewScaleDuration).SetEase(_animationConfig.PointViewScaleEase))
                .AppendInterval(_animationConfig.PointViewMoveDelay)
                .Append(transform.DOMove(targetPosition, moveDuration).SetEase(_animationConfig.PointViewMoveEase))
                .Join(_pointAmount.DOFade(0f, fadeDuration).SetEase(_animationConfig.PointViewFadeEase))
                .Join(_container.DOLocalRotate(new Vector3(0f, rotationAngle, 0f), moveDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear))
                .SetLink(gameObject);

            await sequence.AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);

            gameObject.SetActive(false);
        }

        public void ResetView()
        {
            transform.DOKill();
            _pointAmount.DOKill();
            _container.DOKill();
            transform.localScale = Vector3.one;
            _pointAmount.alpha = 1f;
            _container.localRotation = Quaternion.identity;
        }
    }
}