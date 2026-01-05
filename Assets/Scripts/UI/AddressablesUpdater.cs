using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public static class AddressablesUpdater
{
    public static async UniTask<bool> CheckAndUpdateAsyncOld(bool log = true)
    {
        try
        {
            // 1. Initialize
            await Addressables.InitializeAsync().Task;

            if (log)
                Debug.Log("[Addressables] Initialized");

            // 2. Check catalog updates
            var checkHandle = Addressables.CheckForCatalogUpdates();

            await checkHandle.Task;

            if (!checkHandle.IsValid())
            {
                if (log)
                    Debug.LogWarning("[Addressables] CheckForCatalogUpdates returned invalid handle");
                return false;
            }

            var catalogsToUpdate = checkHandle.Result;

            Addressables.Release(checkHandle);

            if (catalogsToUpdate == null || catalogsToUpdate.Count == 0)
            {
                if (log)
                    Debug.Log("[Addressables] No catalog updates");
                return false;
            }

            if (log)
                Debug.Log($"[Addressables] Updating {catalogsToUpdate.Count} catalogs");

            // 3. Update catalogs
            var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
            await updateHandle.Task;

            if (!updateHandle.IsValid())
            {
                Debug.LogError("[Addressables] UpdateCatalogs returned invalid handle");
                return false;
            }

            var updatedCatalogs = updateHandle.Result;
            
            // 4. Clear cache
            var clearHandle = Addressables.ClearDependencyCacheAsync(updatedCatalogs, false);
            //var clearHandle = Addressables.ClearDependencyCacheAsync(catalogsToUpdate, false);
            await clearHandle.Task;

            if (clearHandle.IsValid())
                Addressables.Release(clearHandle);

            Addressables.Release(updateHandle);

            if (log)
                Debug.Log("[Addressables] Catalogs updated & cache cleared");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
    
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
        await Addressables.ClearDependencyCacheAsync(updatedCatalogs, false).Task;
    
        Addressables.Release(updateHandle);
    
        if (log) Debug.Log("[Addressables] Catalogs updated and cache cleared");
        return true;
    }
}
