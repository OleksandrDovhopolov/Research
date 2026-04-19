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
        private readonly AnimateCurrency _animateCurrency;
        
        public ResourcesCheatModule(ResourceManager resourceManager, AnimateCurrency animateCurrency)
        {
            _resourceManager = resourceManager;
            _animateCurrency = animateCurrency;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add gold", amount =>
            {
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, AnimationTargetTypes.Gold,  amount, null);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Gold));
                _resourceManager.Add(ResourceType.Gold, amount, ResourceManager.CheatAddReason).Forget();
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gold", amount =>
            {
                _resourceManager.Remove(ResourceType.Gold, amount, ResourceManager.CheatRemoveReason).Forget();
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add energy", amount =>
            {
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, AnimationTargetTypes.Energy,  amount, null);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Energy));
                _resourceManager.Add(ResourceType.Energy, amount, ResourceManager.CheatAddReason).Forget();
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove energy", amount =>
            {
                _resourceManager.Remove(ResourceType.Energy, amount, ResourceManager.CheatRemoveReason).Forget();
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add gems", amount =>
            {
                _resourceManager.Add(ResourceType.Gems, amount, ResourceManager.CheatAddReason).Forget();
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, AnimationTargetTypes.Gems,  amount, null);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Gems));
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gems", amount =>
            {
                _resourceManager.Remove(ResourceType.Gems, amount, ResourceManager.CheatRemoveReason).Forget();
            }).WithGroup(SampleGroup));
        }
    }
}

