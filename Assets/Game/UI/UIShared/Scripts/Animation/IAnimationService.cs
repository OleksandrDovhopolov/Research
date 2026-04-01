using UnityEngine;

namespace UIShared
{
    public interface IAnimationService
    {
        void Animate(Vector3 from, int amount, string resourceId, Sprite sprite);
    }
}