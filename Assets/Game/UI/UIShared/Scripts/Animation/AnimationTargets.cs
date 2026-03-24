using System.Collections.Generic;
using CoreResources;
using UnityEngine;

namespace UIShared
{
    public static class AnimationTargets
    {
        private static readonly Dictionary<ResourceType, Transform> Targets = new();

        public static void Register(ResourceType resourceType, Transform targetTransform)
        {
            if (targetTransform == null)
            {
                Debug.LogWarning($"Cannot register animation target for {resourceType}: target transform is null.");
                return;
            }

            Targets[resourceType] = targetTransform;
        }

        public static void Remove(ResourceType resourceType)
        {
            Targets.Remove(resourceType);
        }

        public static Vector3 GetTargetPosition(ResourceType resourceType)
        {
            if (Targets.TryGetValue(resourceType, out var targetTransform) && targetTransform != null)
            {
                return targetTransform.position;
            }

            Debug.LogWarning($"Animation target is not registered for {resourceType}. Returning Vector3.zero.");
            return Vector3.zero;
        }
    }
}