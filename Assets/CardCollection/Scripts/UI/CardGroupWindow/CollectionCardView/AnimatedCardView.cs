using System.Collections;
using UnityEngine;

namespace core
{
    public class AnimatedCardView : CollectionCardView
    {
        [Space, Space, Header("Animations")]
        [SerializeField] private float _animationDuration = 1f;
        [SerializeField] private float _scaleFactor = 1.5f;
        
        private RectTransform _rt;
        private Vector3 _startScale;
        private Coroutine _moveRoutine;
        private Coroutine _scaleRoutine;
        
        public float AnimationDuration => _animationDuration;
        
        protected override void Awake()
        { 
            base.Awake();
            _rt = (RectTransform)_openCardContainer.transform;
            _startScale = _rt.localScale;
        }
                
        public void PlayCardPreview(Vector2 targetPosition)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);

            _moveRoutine = StartCoroutine(AnimateAnchoredPos(_rt.anchoredPosition, targetPosition, _animationDuration));
            _scaleRoutine = StartCoroutine(AnimateScale(_rt.localScale, _startScale * _scaleFactor, _animationDuration));
        }

        public void HideCard(Vector2 targetPosition)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);

            _moveRoutine = StartCoroutine(AnimateAnchoredPos(_rt.anchoredPosition, targetPosition, _animationDuration));
            _scaleRoutine = StartCoroutine(AnimateScale(_rt.localScale, _startScale, _animationDuration));
        }
        
        private IEnumerator AnimateAnchoredPos(Vector2 from, Vector2 to, float duration)
        {
            if (duration <= 0f)
            {
                _rt.anchoredPosition = to;
                yield break;
            }

            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                _rt.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }

            _rt.anchoredPosition = to;
        }

        private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                _rt.localScale = to;
                yield break;
            }

            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                k = 1f - (1f - k) * (1f - k);

                _rt.localScale = Vector3.Lerp(from, to, k);
                yield return null;
            }

            _rt.localScale = to;
        }
    }
}