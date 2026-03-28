using System.Collections;
using UISystem;
using UnityEngine;

namespace core
{
    public class DefaultWindowAnimation : WindowAnimation
    {
        [SerializeField] private float _animationDuration;
        [SerializeField] private CanvasGroup _canvasGroup;

        public override float ShowAnimationTime => _animationDuration;
        
        public override IEnumerator AnimationIn()
        {
            float timer = 0f;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            while (timer < _animationDuration)
            {
                timer += Time.deltaTime;
                float t = timer / _animationDuration;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
        }

        public override IEnumerator AnimationOut(float animationTime)
        {
            float timer = 0f;
            float duration = animationTime <= 0 ? _animationDuration : animationTime;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
        }
    }
}

