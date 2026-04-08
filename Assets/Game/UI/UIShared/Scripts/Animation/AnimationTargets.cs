using System.Collections.Generic;
using UnityEngine;

namespace UIShared
{
    public static class AnimationTargets
    {
        private static readonly Dictionary<string, Transform> Targets = new();

        public static void Register(string targetType, Transform targetTransform)
        {
            if (targetTransform == null)
            {
                Debug.LogWarning($"Cannot register animation target for {targetType}: target transform is null.");
                return;
            }

            Targets[targetType] = targetTransform;
        }

        public static void Remove(string targetType)
        {
            Targets.Remove(targetType);
        }

        public static Vector3 GetTargetPosition(string targetType)
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