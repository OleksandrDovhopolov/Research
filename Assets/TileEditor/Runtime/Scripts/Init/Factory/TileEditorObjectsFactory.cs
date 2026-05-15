using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TileEditor
{
    public class TileEditorObjectsFactory : ILocationObjectsFactory, IDisposable
    {
        private readonly List<GameObject> _prefabRefs = new();

        public Task<LocationObject> Create(LocationObjectModel objectModel, Transform root, CancellationTokenSource cancellationTokenSource = null)
        {
            //var prefabKey = $"{objectModel.objectId}.tile";
            var prefabKey = $"{objectModel.objectId}";
            GameObject prefab;
            try
            {
                prefab = ProdAddressablesWrapper.LoadSync<GameObject>(prefabKey);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Tile Editor] Failed to load location object prefab. objectId: '{objectModel.objectId}', key: '{prefabKey}'. {exception.Message}");
                return Task.FromResult<LocationObject>(null);
            }

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
