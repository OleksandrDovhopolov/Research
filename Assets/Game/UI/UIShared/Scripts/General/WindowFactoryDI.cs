using System;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure;
using UISystem;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace core
{
    public class WindowFactoryDI : WindowFactoryBase
    {
        private const string Tag = "[Window Factory]";
        
        private readonly UIManager _uiManager;
        private readonly IObjectResolver _diContainer;
        
        public WindowFactoryDI(UIManager uiManager, IObjectResolver diContainer)
        {
            _uiManager = uiManager;
            _diContainer = diContainer;
        }
        
        public override async Task<T> CreateAsync<T>()
        {
            var windowAttribute = UIExtension.GetWindowAttribute<T>();

            var window = await ProdAddressablesWrapper.LoadAsync<GameObject>(windowAttribute.PrefabAddressableReference, CancellationToken.None);
            
            if (window == null)
            {
                throw new Exception($"{Tag} Base window at path {windowAttribute.PrefabAddressableReference} does not exist");
            }

            return Create<T>(window.GetComponent<WindowView>(), windowAttribute);
        }

        public override T CreaseSync<T>()
        {
            throw new NotImplementedException("Method not implemented");
            /*Debug.LogWarning("Creating Window CreaseSync");
            
            var windowAttribute = UIExtension.GetWindowAttribute<T>();

            var windowPrefab = AddressablesWrapper.LoadSync<GameObject>(windowAttribute.PrefabAddressableReference);
            if (windowPrefab == null || !windowPrefab.TryGetComponent<WindowView>(out var windowView))
            {
                throw new Exception($"{Tag} Base window at path {windowAttribute.PrefabAddressableReference} does not exist");
            }

            return Create<T>(windowView, windowAttribute);*/
        }

        protected override T Create<T>(WindowView windowPrefab, WindowAttribute windowAttribute)
        {
            windowPrefab.gameObject.SetActive(false);
            var newGo = Instantiate(windowPrefab, _root);
            
            windowPrefab.gameObject.SetActive(true);

            var controller = Activator.CreateInstance<T>();
            _diContainer.Inject(controller);
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