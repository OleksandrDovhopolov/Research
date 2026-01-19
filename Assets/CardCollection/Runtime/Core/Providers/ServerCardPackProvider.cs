using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    public class ServerCardPackProvider : ICardPackProvider
    {
        private readonly string serverUrl;
        private List<CardPackConfig> cachedPacks;
        private bool isInitialized;

        public ServerCardPackProvider(string baseServerUrl)
        {
            serverUrl = baseServerUrl ?? throw new ArgumentNullException(nameof(baseServerUrl));
        }

        public async UniTask<List<CardPackConfig>> GetCardPacksAsync()
        {
            if (isInitialized && cachedPacks != null) return cachedPacks;

            try
            {
                Debug.Log("[ServerCardPackProvider] Requesting card packs from server...");

                // using (var request = UnityWebRequest.Get($"{serverUrl}/api/card-packs"))
                // {
                //     await request.SendWebRequest().ToUniTask();
                //     if (request.result == UnityWebRequest.Result.Success)
                //     {
                //         var json = request.downloadHandler.text;
                //         var config = JsonUtility.FromJson<CardPackConfigList>(json);
                //         cachedPacks = config?.packs ?? new List<CardPackConfig>();
                //     }
                // }

                await UniTask.Delay(500);
                cachedPacks = new List<CardPackConfig>();
                isInitialized = true;

                return cachedPacks;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerCardPackProvider] Error fetching packs: {ex.Message}");
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