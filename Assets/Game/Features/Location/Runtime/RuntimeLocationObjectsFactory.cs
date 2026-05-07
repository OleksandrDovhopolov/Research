using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabros.TileEditor;
using Infrastructure;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Game.Features.Locations
{
    public sealed class RuntimeLocationObjectsFactory : ILocationObjectsFactory, IDisposable
    {
        private readonly List<GameObject> _prefabRefs = new();

        private readonly IObjectResolver _resolver;
        
        public RuntimeLocationObjectsFactory(IObjectResolver resolver)
        {
            _resolver = resolver;
        }
        
        public async Task<LocationObject> Create(
            LocationObjectModel objectModel,
            Transform root,
            CancellationTokenSource cancellationTokenSource = null)
        {
            var prefabKey = objectModel.objectId;
            if (string.IsNullOrWhiteSpace(prefabKey))
            {
                Debug.LogWarning("[Location] Failed to load object prefab. objectId is null or empty.");
                return null;
            }

            GameObject prefab;

            try
            {
                var cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;
                prefab = await ProdAddressablesWrapper.LoadAsync<GameObject>(prefabKey, cancellationToken);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Location] Failed to load object prefab. objectId: '{objectModel.objectId}', key: '{prefabKey}'. {exception.Message}");
                return null;
            }

            _prefabRefs.Add(prefab);
            //var locationObject = Object.Instantiate(prefab, root);
            var locationObject = _resolver.Instantiate(prefab, root);
            
            return locationObject != null ? locationObject.GetComponent<LocationObject>() : null;
        }

        public void Dispose()
        {
            foreach (var prefab in _prefabRefs)
            {
                ProdAddressablesWrapper.Release(prefab);
            }

            _prefabRefs.Clear();
        }
    }
}
