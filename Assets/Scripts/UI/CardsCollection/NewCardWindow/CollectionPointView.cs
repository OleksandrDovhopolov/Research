using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace core
{
    public class CollectionPointView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _pointAmount;
        [SerializeField] private RectTransform _container;
        [SerializeField] private float _scaleDuration = 1f;
        [SerializeField] private float _moveDelay = 0.5f;
        [SerializeField] private float _moveDuration = 1f;
        [SerializeField] private float _rotationSpeed = 360f;

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

            var rotationAngle = _rotationSpeed * _moveDuration;

            var sequence = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one, _scaleDuration).SetEase(Ease.OutBack))
                .AppendInterval(_moveDelay)
                .Append(transform.DOMove(targetPosition, _moveDuration).SetEase(Ease.InQuad))
                .Join(_pointAmount.DOFade(0f, _moveDuration / 2f).SetEase(Ease.OutQuad))
                .Join(_container.DOLocalRotate(new Vector3(0f, rotationAngle, 0f), _moveDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear))
                .SetLink(gameObject);

            await sequence.AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);
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