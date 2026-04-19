using cheatModule;
using CoreResources;
using Cysharp.Threading.Tasks;
using UIShared;
using UnityEngine;

namespace Game.Cheat
{
    public class ResourcesCheatModule : ICheatsModule
    {
        private const string SampleGroup = "Resources";
        
        private readonly ResourceManager _resourceManager;
        private readonly IResourceOperationsService _resourceOperationsService;
        private readonly AnimateCurrency _animateCurrency;
        
        public ResourcesCheatModule(
            ResourceManager resourceManager,
            IResourceOperationsService resourceOperationsService,
            AnimateCurrency animateCurrency)
        {
            _resourceManager = resourceManager;
            _resourceOperationsService = resourceOperationsService;
            _animateCurrency = animateCurrency;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add gold", amount =>
            {
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, AnimationTargetTypes.Gold,  amount, null);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Gold));
                _resourceOperationsService.AddAsync(ResourceType.Gold, amount, ResourceManager.CheatAddReason).Forget();
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gold", amount =>
            {
                _resourceOperationsService.RemoveAsync(ResourceType.Gold, amount, ResourceManager.CheatRemoveReason).Forget();
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add energy", amount =>
            {
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, AnimationTargetTypes.Energy,  amount, null);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Energy));
                _resourceOperationsService.AddAsync(ResourceType.Energy, amount, ResourceManager.CheatAddReason).Forget();
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove energy", amount =>
            {
                _resourceOperationsService.RemoveAsync(ResourceType.Energy, amount, ResourceManager.CheatRemoveReason).Forget();
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add gems", amount =>
            {
                _resourceOperationsService.AddAsync(ResourceType.Gems, amount, ResourceManager.CheatAddReason).Forget();
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, AnimationTargetTypes.Gems,  amount, null);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Gems));
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gems", amount =>
            {
                _resourceOperationsService.RemoveAsync(ResourceType.Gems, amount, ResourceManager.CheatRemoveReason).Forget();
            }).WithGroup(SampleGroup));
        }
    }
}

