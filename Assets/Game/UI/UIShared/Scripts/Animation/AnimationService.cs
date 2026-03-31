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

        public void Animate(Vector3 from, int amount, string resourceId)
        {
            if (Enum.TryParse<ResourceType>(resourceId, true, out var resourceType))
            {
                var animationArgs = new ArgAnimateCurrency(from, resourceType,  amount);
                _animateCurrency.Animate(animationArgs, OnAnimationCompleted);
            }

            return;

            void OnAnimationCompleted()
            {
                _resourceManager.NotifyAmountChanged(resourceType);
            }
        }
    }
}