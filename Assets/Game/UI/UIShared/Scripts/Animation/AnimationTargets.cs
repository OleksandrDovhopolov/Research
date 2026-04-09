using System.Collections.Generic;
using UnityEngine;

namespace UIShared
{
    public enum AnimationTargetTypes
    {
        None,
        Energy,
        Gold,
        Gems,
        Inventory
    }
    
    public static class AnimationTargets
    {
        private static readonly Dictionary<AnimationTargetTypes, Transform> Targets = new();

        public static void Register(AnimationTargetTypes animationTargetType, Transform targetTransform)
        {
            if (targetTransform == null)
            {
                Debug.LogWarning($"Cannot register animation target for {animationTargetType}: target transform is null.");
                return;
            }

            Targets[animationTargetType] = targetTransform;
        }

        public static void Remove(AnimationTargetTypes targetType)
        {
            Targets.Remove(targetType);
        }

        public static Vector3 GetTargetPosition(AnimationTargetTypes targetType)
        {
            if (Targets.TryGetValue(targetType, out var targetTransform) && targetTransform != null)
            {
                return targetTransform.position;
            }

            Debug.LogWarning($"Animation target is not registered for {targetType}. Returning Vector3.zero.");
            return Vector3.zero;
        }
    }
}