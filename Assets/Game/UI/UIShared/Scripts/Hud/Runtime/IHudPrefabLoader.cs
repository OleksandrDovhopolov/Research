using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UIShared
{
    public interface IHudPrefabLoader
    {
        UniTask<GameObject> LoadPrefabAsync(string addressableKey, CancellationToken cancellationToken);
        void ReleasePrefab(GameObject prefab);
    }
}
