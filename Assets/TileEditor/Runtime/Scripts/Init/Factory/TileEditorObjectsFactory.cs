using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fabros.TileEditor;
using Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Module.TileEditor
{
    public class TileEditorObjectsFactory : ILocationObjectsFactory, IDisposable
    {
        private readonly List<GameObject> _prefabRefs = new();

        public Task<LocationObject> Create(LocationObjectModel objectModel, Transform root, CancellationTokenSource cancellationTokenSource = null)
        {
            var prefabKey = $"{objectModel.objectId}.tile";
            var prefab = ProdAddressablesWrapper.LoadSync<GameObject>(prefabKey);

            _prefabRefs.Add(prefab);
            var locationObject = Object.Instantiate(prefab, root);

            return Task.FromResult(locationObject == null ? null : locationObject.GetComponent<LocationObject>());
        }

        public void Dispose()
        {
            foreach (var prefab in _prefabRefs)
            {
                ProdAddressablesWrapper.Release(prefab);
            }
        }
    }
}
