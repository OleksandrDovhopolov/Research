using System.Collections;
using UISystem;
using UnityEngine;

namespace core
{
    public class WidgetInfoAnimation : WindowAnimation
    {
        [SerializeField] private float _showDuration;
        [SerializeField] private float _hideDuration;

        public override float ShowAnimationTime => _showDuration;
        
        public override IEnumerator AnimationIn()
        {
            yield return new WaitForSeconds(_showDuration);
        }
        
        public override IEnumerator AnimationOut(float animationTime)
        {
            yield return new WaitForSeconds(_hideDuration);
        }
    }
}