using cheatModule;
using CoreResources;
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
                var animationArgs = new ArgAnimateCurrency(screenCenter, ResourceType.Gold,  amount);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Gold));
                _resourceManager.Add(ResourceType.Gold, amount);
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gold", amount =>
            {
                _resourceManager.Remove(ResourceType.Gold, amount);
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add energy", amount =>
            {
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, ResourceType.Energy,  amount);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Energy));
                _resourceManager.Add(ResourceType.Energy, amount);
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove energy", amount =>
            {
                _resourceManager.Remove(ResourceType.Energy, amount);
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add gems", amount =>
            {
                _resourceManager.Add(ResourceType.Gems, amount);
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, ResourceType.Gems,  amount);
                _animateCurrency.Animate(animationArgs, () => _resourceManager.NotifyAmountChanged(ResourceType.Gems));
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gems", amount =>
            {
                _resourceManager.Remove(ResourceType.Gems, amount);
            }).WithGroup(SampleGroup));
        }
    }
}

