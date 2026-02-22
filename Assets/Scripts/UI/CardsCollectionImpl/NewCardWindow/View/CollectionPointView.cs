using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace core
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

            var rotationAngle = _animationConfig.PointViewMoveRotationSpeed * _animationConfig.PointViewMoveDuration;

            var sequence = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one, _animationConfig.PointViewScaleDuration).SetEase(Ease.OutBack))
                .AppendInterval(_animationConfig.PointViewMoveDelay)
                .Append(transform.DOMove(targetPosition, _animationConfig.PointViewMoveDuration).SetEase(Ease.InQuad))
                .Join(_pointAmount.DOFade(0f, _animationConfig.PointViewMoveDuration / 2f).SetEase(Ease.OutQuad))
                .Join(_container.DOLocalRotate(new Vector3(0f, rotationAngle, 0f), _animationConfig.PointViewMoveDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear))
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