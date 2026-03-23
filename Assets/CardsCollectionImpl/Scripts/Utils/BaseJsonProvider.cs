using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public abstract class BaseJsonProvider<T> : IStaticDataProvider<T>
    {
        protected abstract string ConfigFileName { get; }
        
        private T _cachedData;
        private bool _isInitialized;
        
        public T Data => _isInitialized 
            ? _cachedData 
            : throw new InvalidOperationException($"[{GetType().Name}] Data not loaded!");
        
        public async UniTask<T> LoadAsync(string fileName, CancellationToken ct = default)
        {
            if (_isInitialized && _cachedData != null) 
                return _cachedData;

            ct.ThrowIfCancellationRequested();

            string jsonText = await LoadRawJsonAsync(ct);

            if (string.IsNullOrEmpty(jsonText))
            {
                _cachedData = CreateDefault();
                _isInitialized = true;
                return _cachedData;
            }

            try
            {
                _cachedData = ParseJson(jsonText);
                _isInitialized = true;
                Debug.Log($"[{GetType().Name}] Loaded data from {ConfigFileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error parsing {ConfigFileName}: {ex.Message}");
                _cachedData = CreateDefault();
                _isInitialized = true;
            }

            return _cachedData;
        }

        protected virtual async UniTask<string> LoadRawJsonAsync(CancellationToken ct)
        {
            var request = Resources.LoadAsync<TextAsset>(ConfigFileName);
            await request.WithCancellation(ct);
            
            var textAsset = request.asset as TextAsset;
            if (textAsset == null)
            {
                Debug.LogError($"Config not found in Resources: {ConfigFileName}");
                return null;
            }
            return textAsset.text;
        }

        protected abstract T ParseJson(string json);
        protected abstract T CreateDefault();

        public void ClearCache()
        {
            Dispose();
        }
        
        protected void Dispose()
        {
            _cachedData = default;
            _isInitialized = false;
        }
    }
}