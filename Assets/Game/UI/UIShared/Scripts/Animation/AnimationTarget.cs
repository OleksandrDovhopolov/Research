using UnityEngine;

namespace UIShared
{
    public class AnimationTarget : MonoBehaviour
    {
        [SerializeField] private AnimationTargetTypes animationTargetType;
        
        public void Awake()
        {
            AnimationTargets.Register(animationTargetType, transform);
        }

        public void OnDestroy()
        {
            AnimationTargets.Remove(animationTargetType);
        }
    }
}