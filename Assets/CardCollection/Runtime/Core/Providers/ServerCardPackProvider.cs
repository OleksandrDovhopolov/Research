using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CardCollection.Core
{
    public class ServerCardPackProvider : ICardPackProvider
    {
        private readonly string _serverUrl;
        private List<CardPackConfig> _cachedPacks;
        private bool _isInitialized;

        public ServerCardPackProvider(string baseServerUrl)
        {
            _serverUrl = baseServerUrl ?? throw new ArgumentNullException(nameof(baseServerUrl));
        }

        public async UniTask<List<CardPackConfig>> GetCardConfigsAsync(CancellationToken ct = default)
        {
            if (_isInitialized && _cachedPacks != null) return _cachedPacks;

            try
            {
                Debug.Log("[ServerCardPackProvider] Requesting card packs from server...");

                // using (var request = UnityWebRequest.Get($"{serverUrl}/api/card-packs"))
                // {
                //     await request.SendWebRequest().ToUniTask(cancellationToken: ct);
                //     if (request.result == UnityWebRequest.Result.Success)
                //     {
                //         var json = request.downloadHandler.text;
                //         var config = JsonUtility.FromJson<CardPackConfigList>(json);
                //         cachedPacks = config?.packs ?? new List<CardPackConfig>();
                //     }
                // }

                await UniTask.Delay(500, cancellationToken: ct);
                _cachedPacks = new List<CardPackConfig>();
                _isInitialized = true;

                return _cachedPacks;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServerCardPackProvider] Error fetching packs: {ex.Message}");
                _cachedPacks = new List<CardPackConfig>();
                _isInitialized = true;
                return _cachedPacks;
            }
        }

        public async UniTask<CardPackConfig> GetCardConfigByIdAsync(string packId, CancellationToken ct = default)
        {
            var allPacks = await GetCardConfigsAsync(ct);
            return allPacks.Find(p => p.packId == packId);
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
