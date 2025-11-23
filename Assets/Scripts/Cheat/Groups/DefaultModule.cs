using cheatModule;
using UnityEngine;

namespace core
{
    public class DefaultModule : ICheatsModule
    {
        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Default Module Sample", () =>
            {
                Debug.LogWarning($"Default module");
            }));
        }
    }
}