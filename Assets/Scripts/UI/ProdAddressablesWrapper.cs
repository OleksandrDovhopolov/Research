using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using System.Threading;

namespace core
{
    public static class ProdAddressablesWrapper
    {
        // key = address
        private static readonly Dictionary<string, AsyncOperationHandle> Cache = new();
        private static readonly object Lock = new();

        // ---------------- LOAD ASYNC ----------------

        public static async Task<T> LoadAsync<T>(string address) where T : Object
        {
            AsyncOperationHandle handle;
            
            lock (Lock)
            {
                if (Cache.TryGetValue(address, out var cachedHandle))
                {
                    handle = cachedHandle;
                }
                else
                {
                    handle = Addressables.LoadAssetAsync<T>(address);
                    Cache[address] = handle;
                }
            }

            await handle.Task;
            return handle.Convert<T>().Result;
        }

        // ---------------- RELEASE ----------------

        public static void Release(string address)
        {
            if (!Cache.TryGetValue(address, out var handle)) return;

            Addressables.Release(handle);
            Cache.Remove(address);
        }

        public static void ReleaseAll()
        {
            foreach (var handle in Cache.Values)
            {
                Addressables.Release(handle);
            }

            Cache.Clear();
        }
    }
}