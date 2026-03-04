using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Infrastructure
{
    public static class AddressablesUpdater
    {
        public static async UniTask<bool> CheckAndUpdateAsync(bool log = false)
        {
            await Addressables.InitializeAsync().Task;

            // --- CHECK ---
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle.Task;

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning("[Addressables] CheckForCatalogUpdates returned invalid handle");
                Addressables.Release(checkHandle);
                return false;
            }

            var catalogsToUpdate = checkHandle.Result;
            Addressables.Release(checkHandle);

            if (catalogsToUpdate == null || catalogsToUpdate.Count == 0)
            {
                if (log) Debug.Log("[Addressables] Catalogs up to date");
                return false;
            }

            // --- UPDATE ---
            var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
            await updateHandle.Task;

            if (!updateHandle.IsValid() || updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[Addressables] UpdateCatalogs failed");
                Addressables.Release(updateHandle);
                return false;
            }

            var updatedCatalogs = updateHandle.Result;

            // --- CLEAR CACHE ---
            var clearHandle = Addressables.ClearDependencyCacheAsync(updatedCatalogs, false);
            await clearHandle.Task;

            if (clearHandle.IsValid())
                Addressables.Release(clearHandle);

            Addressables.Release(updateHandle);

            if (log) Debug.Log("[Addressables] Catalogs updated and cache cleared");
            return true;
        }
    }
}

