using System.Collections;
using UnityEngine;

namespace core
{
    public class AnimatedCardView : CollectionCardView
    {
        [Space, Space, Header("Animations")]
        [SerializeField] private float _animationDuration = 1f;
        [SerializeField] private float _scaleFactor = 1.5f;
        [SerializeField] private GameObject _test;
        
        private RectTransform _openCardRectTransform;
        private RectTransform _closedCardRectTransform;
        private Vector3 _startScale;
        private Coroutine _moveRoutine;
        private Coroutine _scaleRoutine;
        
        public float AnimationDuration => _animationDuration;
        
        protected override void Awake()
        { 
            base.Awake();
            _openCardRectTransform = (RectTransform)_openCardContainer.transform;
            _closedCardRectTransform = (RectTransform)_closedCardContainer.transform;
            _startScale = _openCardRectTransform.localScale;
        }
                
        public void PlayCardPreview(Vector2 targetPosition, bool isOpen)
        {
            var rect = isOpen ? _openCardRectTransform : _closedCardRectTransform;
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);

            _moveRoutine = StartCoroutine(AnimateAnchoredPos(rect, rect.anchoredPosition, targetPosition, _animationDuration));
            _scaleRoutine = StartCoroutine(AnimateScale(rect, rect.localScale, _startScale * _scaleFactor, _animationDuration));
        }

        public void HideCard(Vector2 targetPosition, bool isOpen)
        {
            var rect = isOpen ? _openCardRectTransform : _closedCardRectTransform;
            
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);

            _moveRoutine = StartCoroutine(AnimateAnchoredPos(rect, rect.anchoredPosition, targetPosition, _animationDuration));
            _scaleRoutine = StartCoroutine(AnimateScale(rect, rect.localScale, _startScale, _animationDuration));
        }
        
        private IEnumerator AnimateAnchoredPos(RectTransform rect, Vector2 from, Vector2 to, float duration)
        {
            if (duration <= 0f)
            {
                rect.anchoredPosition = to;
                yield break;
            }

            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                rect.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }

            rect.anchoredPosition = to;
        }

        private IEnumerator AnimateScale(RectTransform rect, Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                rect.localScale = to;
                yield break;
            }

            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                k = 1f - (1f - k) * (1f - k);

                rect.localScale = Vector3.Lerp(from, to, k);
                yield return null;
            }

            rect.localScale = to;
        }
    }
}