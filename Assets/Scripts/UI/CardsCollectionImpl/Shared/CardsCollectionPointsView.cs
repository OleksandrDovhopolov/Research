using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace core
{
    public class CardsCollectionPointsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _pointAmount;
        [SerializeField] private NewCardAnimationConfig _animationConfig;
        
        private int _currentPoints;
        
        public void SetPointsAmount(int pointsAmount)
        {
            _currentPoints = pointsAmount;
            _pointAmount.text = _currentPoints.ToString();
        }
        
        public void UpdatePointsAmount(int pointsAmount)
        {
            _currentPoints += pointsAmount;
            _pointAmount.text = _currentPoints.ToString();
        }
        
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public async UniTask ShowAsync(Vector3 from, Vector3 to, CancellationToken ct)
        {
            transform.position = from;

            await transform.DOMove(to, _animationConfig.PointViewAnimationDuration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject)
                .AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);
        }

        public async UniTask HideAsync(Vector3 from, Vector3 to, CancellationToken ct)
        {
            transform.position = from;

            await transform.DOMove(to, _animationConfig.PointViewAnimationDuration)
                .SetEase(Ease.InBack)
                .SetLink(gameObject)
                .AsyncWaitForCompletion().AsUniTask()
                .AttachExternalCancellation(ct);
        }
    }
}