using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    public class JsonCardPackProvider : ICardPackProvider
    {
        private const string CONFIG_PATH = "CardCollection/card_packs_config";
        private List<CardPackConfig> cachedPacks;
        private bool isInitialized;

        public async UniTask<List<CardPackConfig>> GetCardPacksAsync()
        {
            if (isInitialized && cachedPacks != null) return cachedPacks;

            var configJson = Resources.Load<TextAsset>(CONFIG_PATH);

            if (configJson == null)
            {
                Debug.LogError($"Card pack config not found at path: {CONFIG_PATH}");
                cachedPacks = new List<CardPackConfig>();
                isInitialized = true;
                return cachedPacks;
            }

            try
            {
                var config = JsonUtility.FromJson<CardPackConfigList>(configJson.text);
                cachedPacks = config?.packs ?? new List<CardPackConfig>();
                isInitialized = true;

                Debug.Log($"[JsonCardPackProvider] Loaded {cachedPacks.Count} card packs from JSON");
                return cachedPacks;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonCardPackProvider] Error parsing JSON: {ex.Message}");
                cachedPacks = new List<CardPackConfig>();
                isInitialized = true;
                return cachedPacks;
            }
        }

        public async UniTask<CardPackConfig> GetCardPackByIdAsync(string packId)
        {
            var allPacks = await GetCardPacksAsync();
            return allPacks.Find(p => p.packId == packId);
        }

        public void ClearCache()
        {
            cachedPacks = null;
            isInitialized = false;
        }

        [Serializable]
        private class CardPackConfigList
        {
            public List<CardPackConfig> packs = new();
        }
    }
}