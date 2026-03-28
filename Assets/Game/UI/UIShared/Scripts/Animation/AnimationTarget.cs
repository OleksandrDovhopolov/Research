using CoreResources;
using UnityEngine;

namespace UIShared
{
    public class AnimationTarget : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType;
        
        public void Awake()
        {
            AnimationTargets.Register(resourceType, transform);
        }

        public void OnDestroy()
        {
            AnimationTargets.Remove(resourceType);
        }
    }
}