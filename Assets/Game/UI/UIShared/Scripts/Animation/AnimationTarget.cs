using UnityEngine;

namespace UIShared
{
    public class AnimationTarget : MonoBehaviour
    {
        [SerializeField] private string animationType;
        
        public void Awake()
        {
            AnimationTargets.Register(animationType, transform);
        }

        public void OnDestroy()
        {
            AnimationTargets.Remove(animationType);
        }
    }
}