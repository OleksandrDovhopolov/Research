using System;
using System.Threading;
using CoreResources;
using Cysharp.Threading.Tasks;
using UIShared;
using UnityEngine;

namespace Rewards
{
    public sealed class ResourcePlayerStateSnapshotHandler : IPlayerStateSnapshotHandler
    {
        private readonly AnimateCurrency _animateCurrency;
        private readonly ResourceManager _resourceManager;

        public ResourcePlayerStateSnapshotHandler(ResourceManager resourceManager, AnimateCurrency animateCurrency)
        {
            _animateCurrency = animateCurrency ?? throw new ArgumentNullException(nameof(animateCurrency));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        }

        public async UniTask ApplyAsync(PlayerStateSnapshotDto snapshot, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (snapshot?.Resources == null || snapshot.Resources.Count == 0)
            {
                return;
            }
            
            //TODO bug 1. all resources is in here but not all can be needed. for example Gold / 50 / Energy / 150 / Gems 1700. use Delta ? 
            //TODO bug 2   var screenCenter. show reward window ? 
            //TODO bug 3 animation logic should be here ? 
            var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            foreach (var kvp in snapshot.Resources)
            {
                if (kvp.Value == 0) continue;
             
                Debug.LogWarning($"Test ResourcePlayerStateSnapshotHandler {kvp.Key} / {kvp.Value}");
                if (Enum.TryParse(kvp.Key, true, out AnimationTargetTypes targetType))
                {
                    var animationArgs = new ArgAnimateCurrency(screenCenter, targetType,  kvp.Value, null);
                    _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(GetResourceType(targetType)));
                }
            }
            
            await _resourceManager.ApplySnapshotAsync(snapshot.Resources, ct :ct);
        }

        private ResourceType GetResourceType(AnimationTargetTypes animationTargetTypes)
        {
            switch (animationTargetTypes)
            {
                case AnimationTargetTypes.Energy:
                    return ResourceType.Energy;
                case AnimationTargetTypes.Gold:
                    return ResourceType.Gold;
                case AnimationTargetTypes.Gems:
                    return ResourceType.Gems;
                default:
                    throw new ArgumentOutOfRangeException(nameof(animationTargetTypes), animationTargetTypes, null);
            }
        }
    }
}
