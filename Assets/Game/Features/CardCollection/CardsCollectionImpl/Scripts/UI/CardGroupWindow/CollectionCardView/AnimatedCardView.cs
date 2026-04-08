using System.Collections;
using UnityEngine;

namespace CardCollectionImpl
{
    public class AnimatedCardView : CardView
    {
        [Space, Space, Header("Animations")]
        [SerializeField] private float _animationDuration = 1f;
        [SerializeField] private float _scaleFactor = 1.5f;
        
        private Vector3 _startScale;
        private Coroutine _moveRoutine;
        private Coroutine _scaleRoutine;
        
        public float AnimationDuration => _animationDuration;
        
        protected override void Awake()
        { 
            base.Awake();
            
            _startScale = CardRect.localScale;
        }
        
        public void PlayCardPreview(Vector2 targetPosition)
        {
            var rect = CardRect;
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);

            _moveRoutine = StartCoroutine(AnimateAnchoredPos(rect, rect.anchoredPosition, targetPosition, _animationDuration));
            _scaleRoutine = StartCoroutine(AnimateScale(rect, rect.localScale, _startScale * _scaleFactor, _animationDuration));
        }

        public void HideCard(Vector2 targetPosition)
        {
            var rect = CardRect;
            
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