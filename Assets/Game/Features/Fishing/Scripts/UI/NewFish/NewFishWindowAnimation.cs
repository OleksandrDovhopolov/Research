using System.Collections;
using DG.Tweening;
using UISystem;
using UnityEngine;

namespace Game.Fishing
{
    public sealed class NewFishWindowAnimation : WindowAnimation
    {
        [SerializeField] private CanvasGroup _rootCanvasGroup;
        [SerializeField] private CanvasGroup _backgroundCanvasGroup;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private float _showDuration = 0.28f;
        [SerializeField] private float _hideDuration = 0.18f;
        [SerializeField] private float _contentFadeDelay = 0.03f;
        [SerializeField] private float _backgroundTargetAlpha = 0.6f;
        [SerializeField] private Vector3 _hiddenScale = new Vector3(0.92f, 0.92f, 1f);

        public override float ShowAnimationTime => _showDuration;

        private void Awake()
        {
            ResolveReferences();
        }

        public override IEnumerator AnimationIn()
        {
            ResolveReferences();
            DOTween.Kill(this);

            if (_rootCanvasGroup != null)
            {
                _rootCanvasGroup.alpha = 1f;
                _rootCanvasGroup.interactable = true;
                _rootCanvasGroup.blocksRaycasts = true;
            }

            if (_backgroundCanvasGroup != null)
                _backgroundCanvasGroup.alpha = 0f;

            if (_contentRoot != null)
                _contentRoot.localScale = _hiddenScale;

            var sequence = DOTween.Sequence()
                .SetId(this);

            if (_backgroundCanvasGroup != null)
                sequence.Join(TweenCanvasGroupAlpha(_backgroundCanvasGroup, _backgroundTargetAlpha, _showDuration, Ease.OutQuad));

            if (_contentRoot != null)
            {
                sequence.Insert(_contentFadeDelay, _contentRoot.DOScale(Vector3.one, _showDuration).SetEase(Ease.OutBack));

                var contentCanvasGroup = _contentRoot.GetComponent<CanvasGroup>();
                if (contentCanvasGroup != null)
                {
                    contentCanvasGroup.alpha = 0f;
                    sequence.Insert(_contentFadeDelay, TweenCanvasGroupAlpha(contentCanvasGroup, 1f, _showDuration - _contentFadeDelay, Ease.OutQuad));
                }
            }

            yield return WaitForSequence(sequence);
        }

        public override IEnumerator AnimationOut(float animationTime)
        {
            ResolveReferences();
            DOTween.Kill(this);

            var duration = animationTime <= 0f ? _hideDuration : animationTime;

            if (_rootCanvasGroup != null)
            {
                _rootCanvasGroup.interactable = false;
                _rootCanvasGroup.blocksRaycasts = false;
            }

            var sequence = DOTween.Sequence()
                .SetId(this);

            if (_backgroundCanvasGroup != null)
                sequence.Join(TweenCanvasGroupAlpha(_backgroundCanvasGroup, 0f, duration, Ease.InQuad));

            if (_contentRoot != null)
            {
                sequence.Join(_contentRoot.DOScale(_hiddenScale, duration).SetEase(Ease.InCubic));

                var contentCanvasGroup = _contentRoot.GetComponent<CanvasGroup>();
                if (contentCanvasGroup != null)
                    sequence.Join(TweenCanvasGroupAlpha(contentCanvasGroup, 0f, duration, Ease.InQuad));
            }

            yield return WaitForSequence(sequence);
        }

        private void ResolveReferences()
        {
            _rootCanvasGroup ??= GetComponent<CanvasGroup>();
            _contentRoot ??= FindRectTransform("WindowContainer");

            if (_backgroundCanvasGroup == null)
            {
                var backgroundTransform = transform.Find("PopupBg");
                if (backgroundTransform != null)
                    _backgroundCanvasGroup = backgroundTransform.GetComponent<CanvasGroup>();
            }
        }

        private RectTransform FindRectTransform(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<RectTransform>() : null;
        }

        private static IEnumerator WaitForSequence(Sequence sequence)
        {
            if (sequence == null)
                yield break;

            while (sequence.IsActive() && sequence.IsPlaying())
                yield return null;
        }

        private static Tween TweenCanvasGroupAlpha(CanvasGroup canvasGroup, float targetAlpha, float duration, Ease ease)
        {
            return DOTween
                .To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, targetAlpha, duration)
                .SetEase(ease);
        }
    }
}
