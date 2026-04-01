using System;
using CoreResources;
using UnityEngine;

namespace UIShared
{
    public class AnimationService : IAnimationService
    {
        private readonly ResourceManager _resourceManager;
        private readonly AnimateCurrency _animateCurrency;

        public AnimationService(AnimateCurrency animateCurrency, ResourceManager resourceManager)
        {
            _animateCurrency =  animateCurrency;
            _resourceManager =  resourceManager;
        }

        public void Animate(Vector3 from, int amount, string resourceId, Sprite sprite)
        {
            var animationTargetId = Enum.TryParse<ResourceType>(resourceId, true, out var resourceType) ? resourceType.ToString() : "Inventory";
            var animationArgs = new ArgAnimateCurrency(from, animationTargetId,  amount, sprite);
            _animateCurrency.Animate(animationArgs, OnAnimationCompleted);

            return;

            void OnAnimationCompleted()
            {
                if (Enum.TryParse<ResourceType>(resourceId, true, out var resource))
                {
                    _resourceManager.NotifyAmountChanged(resource);
                }
            }
        }
    }
}