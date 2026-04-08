using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Firebase.RemoteConfig;
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
            ct.ThrowIfCancellationRequested();

            await UniTask.CompletedTask;
            var value = FirebaseRemoteConfig.DefaultInstance.GetValue(address);
            var json = value.StringValue;

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[{nameof(BaseFirebaseProvider<T, TK>)}] Remote Config value for key '{address}' is empty.");
                return null;
            }

            var textAsset = new TextAsset(json);
            return textAsset as TK;
        }

        protected abstract T ParseAsset(TK asset);

        public void ClearCache()
        {
            Dispose();
        }

        private void Dispose()
        {
            _cachedData = default;
            _isInitialized = false;
            _loadedAddress = null;
        }
    }
}