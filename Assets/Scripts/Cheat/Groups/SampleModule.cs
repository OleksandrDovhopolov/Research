using cheatModule;
using UnityEngine;

namespace core
{
    public class SampleModule : ICheatsModule
    {
        private const string SampleGroup = "Sample";

        private readonly ICollectionUpdater _collectionUpdater;
        
        public SampleModule(ICollectionUpdater collectionUpdater)
        {
            _collectionUpdater = collectionUpdater;
        }

        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Sample Module", () =>
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
            
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Input int", id =>
            {
                Debug.LogWarning($"Sample int {id}");
            }).WithGroup(SampleGroup));
            
            cheatsContainer.AddItem<CheatInputItemWithLabel>(item => item.OnInputChange<float>("test", val =>
            {
                Debug.LogWarning($"Sample wi label val => {val}");
            }).WithLabel("Sample").WithGroup(SampleGroup));
        }
    }
}

