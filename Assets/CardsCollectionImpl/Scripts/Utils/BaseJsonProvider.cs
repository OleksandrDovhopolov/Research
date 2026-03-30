using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollectionImpl
{
    public abstract class BaseJsonProvider<T> : IStaticDataProvider<T>
    {
        private T _cachedData;
        private bool _isInitialized;
        private string _loadedFileName;
        
        public T Data => _isInitialized 
            ? _cachedData 
            : throw new InvalidOperationException($"[{GetType().Name}] Data not loaded!");
        
        /*public async UniTask<T> LoadAsync(string fileName, CancellationToken ct = default)
        {
            if (_isInitialized && _cachedData != null) 
                return _cachedData;

            ct.ThrowIfCancellationRequested();

            string jsonText = await LoadRawJsonAsync(fileName, ct);

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
                Debug.Log($"[{GetType().Name}] Loaded data from {fileName}");
            }
            catch (Exception ex)
            {
                //TODO remove CreateDefault and _isInitialized. only throw error 
                Debug.LogError($"[{GetType().Name}] Error parsing {fileName}: {ex.Message}");
                _cachedData = CreateDefault();
                _isInitialized = true;
            }

            return _cachedData;
        }*/
        
        public async UniTask<T> LoadAsync(string fileName, CancellationToken ct = default)
        {
            if (_isInitialized &&
                _cachedData != null &&
                string.Equals(_loadedFileName, fileName, StringComparison.Ordinal))
            {
                return _cachedData;
            }

            ct.ThrowIfCancellationRequested();

            string jsonText = await LoadRawJsonAsync(fileName, ct);

            if (string.IsNullOrEmpty(jsonText))
            {
                _cachedData = CreateDefault();
                _isInitialized = true;
                _loadedFileName = fileName;
                return _cachedData;
            }

            try
            {
                _cachedData = ParseJson(jsonText);
                _isInitialized = true;
                _loadedFileName = fileName;
                Debug.Log($"[{GetType().Name}] Loaded data from {fileName}");
            }
            catch (Exception ex)
            {
                //TODO remove CreateDefault and _isInitialized. only throw error 
                Debug.LogError($"[{GetType().Name}] Error parsing {fileName}: {ex.Message}");
                _cachedData = CreateDefault();
                _isInitialized = true;
                _loadedFileName = fileName;
            }
            
            /*_cachedData = ParseJson(jsonText);
            _isInitialized = true;
            _loadedFileName = fileName;*/
            return _cachedData;
        }

        private async UniTask<string> LoadRawJsonAsync(string fileName, CancellationToken ct)
        {
            var request = Resources.LoadAsync<TextAsset>(fileName);
            await request.WithCancellation(ct);
            
            var textAsset = request.asset as TextAsset;
            if (textAsset == null)
            {
                Debug.LogError($"Config not found in Resources: {fileName}");
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
        
        private void Dispose()
        {
            _cachedData = default;
            _isInitialized = false;
            _loadedFileName = null;
        }
    }
}