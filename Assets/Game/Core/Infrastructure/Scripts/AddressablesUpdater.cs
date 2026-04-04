using System;
using System.Threading;
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
            return await CheckAndUpdateAsync(CancellationToken.None, null, log);
        }

        public static async UniTask<bool> CheckAndUpdateAsync(
            CancellationToken ct,
            IProgress<float> progress = null,
            bool log = false)
        {
            ct.ThrowIfCancellationRequested();
            progress?.Report(0.05f);
            await Addressables.InitializeAsync().Task.AsUniTask().AttachExternalCancellation(ct);
            progress?.Report(0.2f);

            // --- CHECK ---
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle.Task.AsUniTask().AttachExternalCancellation(ct);
            progress?.Report(0.4f);

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
                progress?.Report(1f);
                return false;
            }

            // --- UPDATE ---
            var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
            await updateHandle.Task.AsUniTask().AttachExternalCancellation(ct);
            progress?.Report(0.8f);

            if (!updateHandle.IsValid() || updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[Addressables] UpdateCatalogs failed");
                Addressables.Release(updateHandle);
                return false;
            }

            var updatedCatalogs = updateHandle.Result;

            // --- CLEAR CACHE ---
            var clearHandle = Addressables.ClearDependencyCacheAsync(updatedCatalogs, false);
            await clearHandle.Task.AsUniTask().AttachExternalCancellation(ct);

            if (clearHandle.IsValid())
                Addressables.Release(clearHandle);

            Addressables.Release(updateHandle);

            if (log) Debug.Log("[Addressables] Catalogs updated and cache cleared");
            progress?.Report(1f);
            return true;
        }
    }
}

