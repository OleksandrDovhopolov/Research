using System;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure;
using UISystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIShared
{
    public class WindowFactoryDI : WindowFactoryBase
    {
        private const string Tag = "[Window Factory]";
        
        private readonly UIManager _uiManager;
        private IObjectResolver _diContainer;
        
        public WindowFactoryDI(UIManager uiManager, IObjectResolver diContainer)
        {
            _uiManager = uiManager;
            _diContainer = diContainer;
        }
        
        public void SetResolver(IObjectResolver resolver)
        {
            _diContainer = resolver;
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
        }

        protected override T Create<T>(WindowView windowPrefab, WindowAttribute windowAttribute)
        {
            windowPrefab.gameObject.SetActive(false);
            var newGo = _diContainer.Instantiate(windowPrefab, _root);
            windowPrefab.gameObject.SetActive(true);

            var controller = Activator.CreateInstance<T>();
            _diContainer.Inject(controller);
            controller.Configurate(newGo, _uiManager, windowAttribute);

            return controller;
        }
    }
}