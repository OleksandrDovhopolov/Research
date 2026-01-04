using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace core
{
    public static class AddressablesWrapper
    {
        private static readonly Dictionary<string, AssetReference> Cache = new();

        public static Task<T> LoadFromTask<T>(string key) where T : Object
        {
            var ar = Cache.TryGetValue(key, out var value) ? value : new AssetReference(key);
            
            return LoadFromTask<T>(ar);
        }
        
        public static Task<T> LoadFromTask<T>(AssetReference assetRef) where T : Object
        {
            Cache.TryAdd(assetRef.AssetGUID, assetRef);
            
            if (assetRef.Asset != null)
                return Task.FromResult(assetRef.Asset as T);
            
            if (assetRef.IsValid())
                return assetRef.OperationHandle.Convert<T>().Task;

            return assetRef.LoadAssetAsync<T>().Task;
        }
        
        public static T LoadSync<T>(string key) where T : Object
        {
            var ar = Cache.TryGetValue(key, out var value) ? value : new AssetReference(key);
            
            return LoadSync<T>(new AssetReference(key));
        }
        
        public static T LoadSync<T>(AssetReference assetRef) where T : Object
        {
            Cache.TryAdd(assetRef.AssetGUID, assetRef);

            if (assetRef.Asset != null)
                return assetRef.Asset as T;
            
            if (assetRef.IsValid())
                return assetRef.OperationHandle.Convert<T>().WaitForCompletion();

            return assetRef.LoadAssetAsync<T>().WaitForCompletion();
        }
        
        public static void ReleaseLoaded()
        {
            foreach (var handle in Cache.Values)
            {
                handle.ReleaseAsset();
            }

            Cache.Clear();
        }

        public static void Release(object obj)
        {
            CleanHandles(obj);

            if (obj is AssetReference assetReference)
                assetReference.ReleaseAsset();
            else
                Addressables.Release(obj);
        }

        public static void ReleaseInstance(GameObject obj)
        {
            CleanHandles(obj);
            Addressables.ReleaseInstance(obj);
        }

        private static void CleanHandles(object obj)
        {
            foreach (var handle in Cache)
            {
                if (!ReferenceEquals(handle.Value, obj)) continue;
                Cache.Remove(handle.Key);
                return;
            }
        }
    }
}
