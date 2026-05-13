using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;

namespace UIShared
{
    public sealed class AddressablesHudPrefabLoader : IHudPrefabLoader
    {
        public async UniTask<GameObject> LoadPrefabAsync(string addressableKey, CancellationToken cancellationToken)
        {
            return await ProdAddressablesWrapper.LoadAsync<GameObject>(addressableKey, cancellationToken);
        }

        public void ReleasePrefab(GameObject prefab)
        {
            if (prefab != null)
                ProdAddressablesWrapper.Release(prefab);
        }
    }
}
