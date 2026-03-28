using cheatModule;
using CoreResources;
using UIShared;
using UnityEngine;

namespace core
{
    public class SampleModule : ICheatsModule
    {
        private const string SampleGroup = "Sample";
        
        private readonly ResourceManager _resourceManager;
        private readonly AnimateCurrency _animateCurrency;
        
        public SampleModule(ResourceManager resourceManager, AnimateCurrency animateCurrency)
        {
            _resourceManager = resourceManager;
            _animateCurrency = animateCurrency;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            /*cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Sample Module", () =>
            {
                Debug.LogWarning($"Sample Module");
            }).WithGroup(SampleGroup));
            
            var sampleList = new[] { "1m", "10m", "30m", "1h", "2h", "3h", "4h", "5h", "8h", "12h", "24h", "7d" };
            cheatsContainer.AddItem<CheatDropdownButtonItem>(item => item
                .SetOptions(sampleList)
                .OnClick("Test items", () =>
                {
                    var name = sampleList[item.CurIndex];
                    Debug.LogWarning($"Sample dropdown {name}");
                }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItemWithLabel>(item => item.OnInputChange<float>("test", val =>
            {
                Debug.LogWarning($"Sample wi label val => {val}");
            }).WithLabel("Sample").WithGroup(SampleGroup));*/
            
            
            //--------------------- Resources ------------------------ //
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add gold", amount =>
            {
                
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
                var animationArgs = new ArgAnimateCurrency(screenCenter, ResourceType.Gold,  amount);
                _animateCurrency.Animate(animationArgs);
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
                _animateCurrency.Animate(animationArgs);
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
                _animateCurrency.Animate(animationArgs);
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove gems", amount =>
            {
                _resourceManager.Remove(ResourceType.Gems, amount);
            }).WithGroup(SampleGroup));
            
            //--------------------- Resources ------------------------ //
        }
    }
}

