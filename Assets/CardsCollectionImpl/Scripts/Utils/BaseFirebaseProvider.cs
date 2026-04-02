using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using UnityEngine.ResourceManagement.Exceptions;
using Object = UnityEngine.Object;

namespace CardCollectionImpl
{
    public abstract class BaseFirebaseProvider<T,TK> : IStaticDataProvider<T> where TK : Object
    {
        private T _cachedData;
        private bool _isInitialized;
        private string _loadedAddress;
        private TK _loadedAsset;

        public T Data => _isInitialized
            ? _cachedData
            : throw new InvalidOperationException($"[{GetType().Name}] Data not loaded!");

        public async UniTask<T> LoadAsync(string address, CancellationToken ct = default)
        {
            if (_isInitialized &&
                _cachedData != null &&
                string.Equals(_loadedAddress, address, StringComparison.Ordinal))
            {
                return _cachedData;
            }

            if (_isInitialized && !string.Equals(_loadedAddress, address, StringComparison.Ordinal))
            {
                Dispose();
            }

            ct.ThrowIfCancellationRequested();

            var loadedAsset = await LoadRawAssetAsync(address, ct);
            _loadedAsset = loadedAsset;
            
            try
            {
                _cachedData = ParseAsset(loadedAsset);
                _isInitialized = true;
                _loadedAddress = address;
                Debug.Log($"[{GetType().Name}] Loaded data from {address}");
            }
            catch (Exception ex)
            {
                throw new OperationException(($"[{GetType().Name}] Error parsing {address}: {ex.Message}"));
            }

            return _cachedData;
        }

        private static async UniTask<TK> LoadRawAssetAsync(string address, CancellationToken ct)
        {
            try
            {
                return await ProdAddressablesWrapper.LoadAsync<TK>(address, ct).AsUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Asset not found in Addressables: {address}. Error: {ex.Message}");
                return null;
            }
        }

        protected abstract T ParseAsset(TK asset);

        public void ClearCache()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (_loadedAsset != null)
            {
                ProdAddressablesWrapper.Release(_loadedAsset);
                _loadedAsset = null;
            }

            _cachedData = default;
            _isInitialized = false;
            _loadedAddress = null;
        }
    }
}