using System;
using System.Threading.Tasks;
using Infrastructure;
using UISystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace core
{
    public class WindowFactoryDI : WindowFactoryBase
    {
        private const string Tag = "[Window Factory]";


        private UIManager _uiManager;
        
        public WindowFactoryDI(UIManager uiManager)
        {
            _uiManager = uiManager;
        }
        
        public override async Task<T> CreateAsync<T>()
        {
            var windowAttribute = UIExtension.GetWindowAttribute<T>();

            var window = await AddressablesWrapper.LoadFromTask<GameObject>(windowAttribute.PrefabAddressableReference);
            
            if (window == null)
            {
                throw new Exception($"{Tag} Base window at path {windowAttribute.PrefabAddressableReference} does not exist");
            }

            return Create<T>(window.GetComponent<WindowView>(), windowAttribute);
        }

        public override T CreaseSync<T>()
        {
            Debug.LogWarning("Creating Window CreaseSync");
            
            var windowAttribute = UIExtension.GetWindowAttribute<T>();

            var windowPrefab = AddressablesWrapper.LoadSync<GameObject>(windowAttribute.PrefabAddressableReference);
            if (windowPrefab == null || !windowPrefab.TryGetComponent<WindowView>(out var windowView))
            {
                throw new Exception($"{Tag} Base window at path {windowAttribute.PrefabAddressableReference} does not exist");
            }

            return Create<T>(windowView, windowAttribute);
        }

        protected override T Create<T>(WindowView windowPrefab, WindowAttribute windowAttribute)
        {
            windowPrefab.gameObject.SetActive(false);
            //var newGo = _placeholderFactory.Create(windowPrefab, _root);
            var newGo = Instantiate(windowPrefab, _root);
            
            windowPrefab.gameObject.SetActive(true);

            var controller = Activator.CreateInstance<T>();
            controller.Configurate(newGo, _uiManager, windowAttribute);

            return controller;
        }

        private static T Instantiate<T>(T prefab, Transform parent, bool worldPositionStays = false)
            where T : Component
        {
            var wasActive = prefab.gameObject.activeSelf;
            prefab.gameObject.SetActive(false);

            var instance = Object.Instantiate(prefab, parent, worldPositionStays);

            instance.name = prefab.name;

            prefab.gameObject.SetActive(wasActive);
            instance.gameObject.SetActive(wasActive);

            return instance;
        }
    }
}