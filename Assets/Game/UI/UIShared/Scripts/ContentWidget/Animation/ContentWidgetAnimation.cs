using System.Collections;
using DG.Tweening;
using UISystem;
using UnityEngine;

namespace UIShared
{
    public class ContentWidgetWindowAnimation : WindowAnimation
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _showAnimationDuration;
        [SerializeField] private float _hideAnimationDuration;

        public override float ShowAnimationTime => _showAnimationDuration;

        public override IEnumerator AnimationIn()
        {
            DOTween.Kill(this);

            transform.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            yield return RunAnimation(Vector3.one, 1f, _showAnimationDuration, Ease.OutBack, Ease.InCirc);
        }

        public override IEnumerator AnimationOut(float animationTime)
        {
            DOTween.Kill(this);

            var duration = animationTime <= 0f ? _hideAnimationDuration : animationTime;
            
            transform.localScale = Vector3.one;
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            yield return RunAnimation(Vector3.zero, 0f, duration, Ease.InQuad, Ease.OutCirc);
        }

        private IEnumerator RunAnimation(Vector3 toScale, float toAlpha, float duration, Ease scaleEase, Ease fadeEase)
        {
            var isCompleted = false;

            var sequence = DOTween.Sequence();
            sequence
                .Append(transform.DOScale(toScale, duration).SetEase(scaleEase))
                .Join(_canvasGroup.DOFade(toAlpha, duration).SetEase(fadeEase))
                .OnComplete(() => isCompleted = true)
                .SetId(this);

            while (!isCompleted && sequence.IsActive())
            {
                yield return null;
            }
        }
    }
}