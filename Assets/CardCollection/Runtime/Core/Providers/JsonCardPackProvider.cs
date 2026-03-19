using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    //TODO should be removed from CardCollection.Core to CardCollectionImpl
    public class JsonCardPackProvider : ICardPackProvider
    {
        private const string CONFIG_FILE_NAME = "card_packs_config";
        private List<CardPackConfig> _cachedPacks;
        private bool _isInitialized;

        public async UniTask<List<CardPackConfig>> GetCardConfigsAsync(CancellationToken ct = default)
        {
            if (_isInitialized && _cachedPacks != null) return _cachedPacks;

            ct.ThrowIfCancellationRequested();

            var configJson = Resources.Load<TextAsset>(CONFIG_FILE_NAME);

            if (configJson == null)
            {
                Debug.LogError($"Card pack config not found at path: {CONFIG_FILE_NAME}");
                _cachedPacks = new List<CardPackConfig>();
                _isInitialized = true;
                return _cachedPacks;
            }

            try
            {
                var config = JsonUtility.FromJson<CardPackConfigList>(configJson.text);
                _cachedPacks = config?.packs ?? new List<CardPackConfig>();
                _isInitialized = true;

                Debug.Log($"[JsonCardPackProvider] Loaded {_cachedPacks.Count} card packs from JSON");
                return _cachedPacks;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonCardPackProvider] Error parsing JSON: {ex.Message}");
                _cachedPacks = new List<CardPackConfig>();
                _isInitialized = true;
                return _cachedPacks;
            }
        }

        public void ClearCache()
        {
            _cachedPacks = null;
            _isInitialized = false;
        }

        [Serializable]
        private class CardPackConfigList
        {
            public List<CardPackConfig> packs = new();
        }
    }
}
